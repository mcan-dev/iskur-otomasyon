using System.Text;
using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class LeaveServiceTests
{
    private static CreateLeaveRequest Leave(int fromDay, int toDay, LeaveType type = LeaveType.Excuse) => new()
    {
        Type = type,
        StartDate = new DateOnly(2026, 11, fromDay),
        EndDate = new DateOnly(2026, 11, toDay),
        Reason = "Aile ziyareti"
    };

    private static (TestDb Db, SeedResult Seed) StudentSession()
    {
        var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        return (db, seed);
    }

    private static DocumentUpload Upload(string fileName, string content = "içerik")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new DocumentUpload { Content = new MemoryStream(bytes), FileName = fileName, Length = bytes.Length };
    }

    [Fact]
    public async Task Request_CountsWeekdays_WhenThereIsNoPlan()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        using var scope = db.NewScope();

        var result = await scope.Leaves.RequestAsync(Leave(2, 6));

        Assert.Equal(LeaveStatus.Pending, result.Leave.Status);
        Assert.Equal(5m, result.Leave.DaysCounted);
        Assert.Equal(5m, result.Balance.Pending);
        Assert.Equal(10m, result.Balance.Remaining);
        Assert.Equal(new DateOnly(2026, 10, 21), result.Leave.AppliedOn);
    }

    [Fact]
    public async Task Request_CountsOnlyPlannedDays_WhenAPlanExists()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.WorkPlans.SaveMyPlanAsync(new SaveWorkPlanRequest
            {
                Year = 2026, Month = 11,
                Days = new[] { 3, 10, 17, 24 }.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 11, d) }).ToList()
            });

        using (var scope = db.NewScope())
        {
            var result = await scope.Leaves.RequestAsync(Leave(2, 6));
            Assert.Equal(1m, result.Leave.DaysCounted);
        }
    }

    [Fact]
    public async Task Request_HolidaysAreNotCounted()
    {
        var (db, seed) = StudentSession();
        using var _ = db;
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Calendar.AddHolidayAsync(new HolidayRequest { Date = new DateOnly(2026, 11, 4), Name = "Deneme tatili" });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            Assert.Equal(4m, (await scope.Leaves.RequestAsync(Leave(2, 6))).Leave.DaysCounted);
    }

    [Fact]
    public async Task Request_HourlyLeave_CountsFractionOfDay()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        var request = Leave(3, 3);
        request.Hours = 3.75m;
        using var scope = db.NewScope();

        var result = await scope.Leaves.RequestAsync(request);

        Assert.Equal(0.5m, result.Leave.DaysCounted);
    }

    [Fact]
    public async Task Request_OverLimit_IsAllowedButWarns()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        using var scope = db.NewScope();

        var result = await scope.Leaves.RequestAsync(Leave(2, 17));

        Assert.Equal(12m, result.Leave.DaysCounted);
        Assert.Contains(result.Balance.Warnings, w => w.Code == "LeavePendingExceeds");
    }

    [Theory]
    [InlineData(6, 2)]
    [InlineData(7, 8)]
    public async Task Request_WithInvalidDates_IsRejected(int fromDay, int toDay)
    {
        var (db, _) = StudentSession();
        using var _ = db;
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.RequestAsync(Leave(fromDay, toDay)));
    }

    [Fact]
    public async Task Request_CannotSpanTwoYears_OrMixHoursWithMultipleDays()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        using var scope = db.NewScope();

        var acrossYears = Leave(2, 6);
        acrossYears.EndDate = new DateOnly(2027, 1, 4);
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.RequestAsync(acrossYears));

        var hourlyRange = Leave(2, 3);
        hourlyRange.Hours = 2;
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.RequestAsync(hourlyRange));

        var hourlyFullDay = Leave(3, 3);
        hourlyFullDay.Hours = 7.5m;
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.RequestAsync(hourlyFullDay));
    }

    [Fact]
    public async Task Request_OverlappingActiveLeave_IsRejected_ButCancelledOneIsNot()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        LeaveResultDto first;
        using (var scope = db.NewScope())
            first = await scope.Leaves.RequestAsync(Leave(2, 4));

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.RequestAsync(Leave(4, 6)));

        using (var scope = db.NewScope())
            await scope.Leaves.CancelAsync(first.Leave.Id);

        using (var scope = db.NewScope())
            await scope.Leaves.RequestAsync(Leave(4, 6));
    }

    [Fact]
    public async Task Request_ForPassiveStudent_IsRejected()
    {
        var (db, seed) = StudentSession();
        using var _ = db;
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Students.EndParticipationAsync(seed.MehmetId, new EndParticipationRequest { Reason = "Ayrıldı" });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.RequestAsync(Leave(2, 4)));
    }

    [Fact]
    public async Task Review_Approve_MovesDaysFromPendingToUsed()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(Leave(2, 4));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var result = await scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true });

            Assert.Equal(LeaveStatus.Approved, result.Leave.Status);
            Assert.Equal("Ayşe Kaya", result.Leave.ReviewedByName);
            Assert.NotNull(result.Leave.ReviewedAt);
            Assert.Equal(3m, result.Balance.Used);
            Assert.Equal(0m, result.Balance.Pending);
            Assert.Equal(7m, result.Balance.Remaining);
        }
    }

    [Fact]
    public async Task Review_Reject_NeedsReason_AndCannotBeRepeated()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(Leave(2, 4));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = false }));

            var result = await scope.Leaves.ReviewAsync(requested.Leave.Id,
                new ReviewLeaveRequest { Approve = false, Note = "Dönem sınavı var" });
            Assert.Equal(LeaveStatus.Rejected, result.Leave.Status);
            Assert.Equal(0m, result.Balance.Pending);

            await Assert.ThrowsAsync<ConflictException>(() =>
                scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true }));
        }
    }

    [Fact]
    public async Task Review_IsOnlyForFacultyAdmin()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(Leave(2, 4));

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true }));
    }

    [Fact]
    public async Task Cancel_OnlyOwnPendingRequests()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(Leave(2, 4));

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.CancelAsync(requested.Leave.Id));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.CancelAsync(requested.Leave.Id));
    }

    [Fact]
    public async Task Visibility_FollowsRoleScope()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(Leave(2, 4));
        var query = new LeaveSearchQuery { Year = 2026 };

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.Leaves.SearchAsync(query));

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.Leaves.SearchAsync(query));

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.Leaves.SearchAsync(query));

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
        {
            Assert.Empty(await scope.Leaves.SearchAsync(query));
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.GetByIdAsync(requested.Leave.Id));
        }

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.GetByIdAsync(requested.Leave.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Leaves.SearchAsync(query));
            Assert.Empty(await scope.Leaves.ListMineAsync(2026));
        }
    }

    [Fact]
    public async Task Document_CanBeAttached_AndIsAuditedWhenStaffOpensIt()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
        {
            requested = await scope.Leaves.RequestAsync(Leave(2, 4, LeaveType.Health));
            var attached = await scope.Leaves.AttachDocumentAsync(requested.Leave.Id, Upload("rapor.pdf"));
            Assert.True(attached.HasDocument);
            Assert.Equal("rapor.pdf", attached.DocumentFileName);
        }

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var download = await scope.Leaves.OpenDocumentAsync(requested.Leave.Id);
            Assert.Equal("application/pdf", download.ContentType);
            using var reader = new StreamReader(download.Content);
            Assert.Equal("içerik", await reader.ReadToEndAsync());
        }

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LeaveDocumentViewed));
    }

    [Fact]
    public async Task Document_RejectsWrongTypeEmptyFilesAndOutsiders()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
        {
            requested = await scope.Leaves.RequestAsync(Leave(2, 4));
            await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.AttachDocumentAsync(requested.Leave.Id, Upload("virus.exe")));
            await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.AttachDocumentAsync(requested.Leave.Id, Upload("bos.pdf", "")));
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.OpenDocumentAsync(requested.Leave.Id));
        }

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.AttachDocumentAsync(requested.Leave.Id, Upload("rapor.pdf")));
    }

    [Fact]
    public async Task Alerts_ListStudentsNearTheLimit_WithinRoleScope()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(Leave(2, 12));   // 9 gün

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            Assert.Empty(await scope.Leaves.GetAlertsAsync(2026));   // henüz onaylanmadı, yalnızca bekliyor
            await scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true });

            var alert = Assert.Single(await scope.Leaves.GetAlertsAsync(2026));
            Assert.Equal("Mehmet Öztürk", alert.StudentName);
            Assert.Contains(alert.Balance.Warnings, w => w.Code == "LeaveNearLimit");
        }

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
            Assert.Empty(await scope.Leaves.GetAlertsAsync(2026));
    }

    [Fact]
    public async Task Policy_ChangeTakesEffectWithoutCodeChange()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            Assert.Equal(10, (await scope.LeaveSettings.GetAsync()).LeaveDayLimit);
            await scope.LeaveSettings.UpdateAsync(new LeavePolicyDto
            {
                LeaveDayLimit = 3, LeaveWarningRemainingDays = 1
            });
        }

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
        {
            var result = await scope.Leaves.RequestAsync(Leave(2, 6));
            Assert.Equal(3, result.Balance.Limit);
            Assert.Contains(result.Balance.Warnings, w => w.Code == "LeavePendingExceeds");
        }
    }

    [Fact]
    public async Task Policy_OnlyFacultyAdminMayChangeIt()
    {
        var (db, seed) = StudentSession();
        using var _ = db;
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        Assert.Equal(10, (await scope.LeaveSettings.GetAsync()).LeaveDayLimit);
        await Assert.ThrowsAsync<ForbiddenException>(() => scope.LeaveSettings.UpdateAsync(new LeavePolicyDto { LeaveDayLimit = 99 }));
    }
}

public class AbsenceServiceTests
{
    private static RecordAbsenceRequest Absent(int studentId, int day = 10, AbsenceType type = AbsenceType.Unexcused) => new()
    {
        StudentId = studentId,
        Date = new DateOnly(2026, 10, day),
        Type = type
    };

    [Fact]
    public async Task Record_StoresWhoRecordedIt_AndRejectsDuplicatesAndFutureDates()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        var recorded = await scope.Absences.RecordAsync(Absent(seed.MehmetId));
        Assert.Equal("Ayşe Kaya", recorded.RecordedByName);

        await Assert.ThrowsAsync<ConflictException>(() => scope.Absences.RecordAsync(Absent(seed.MehmetId)));

        var future = Absent(seed.MehmetId);
        future.Date = new DateOnly(2026, 10, 25);
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Absences.RecordAsync(future));
    }

    [Fact]
    public async Task Record_IsOnlyForFacultyAdmin()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Absences.RecordAsync(Absent(seed.MehmetId)));
    }

    [Fact]
    public async Task Revoke_KeepsTheRecord_FreesTheDay_AndLeavesTheBalance()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        AbsenceDto recorded;
        using (var scope = db.NewScope())
            recorded = await scope.Absences.RecordAsync(Absent(seed.MehmetId));

        using (var scope = db.NewScope())
        {
            Assert.Equal(1m, (await scope.Leaves.GetStudentBalanceAsync(seed.MehmetId, 2026)).AbsenceDays);

            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.Absences.RevokeAsync(recorded.Id, new RevokeAbsenceRequest { Reason = "" }));
            var revoked = await scope.Absences.RevokeAsync(recorded.Id, new RevokeAbsenceRequest { Reason = "Yanlış öğrenci girildi" });
            Assert.True(revoked.IsRevoked);
            Assert.Equal("Ayşe Kaya", revoked.RevokedByName);

            await Assert.ThrowsAsync<ConflictException>(() =>
                scope.Absences.RevokeAsync(recorded.Id, new RevokeAbsenceRequest { Reason = "Tekrar" }));
        }

        using (var scope = db.NewScope())
        {
            Assert.Equal(0m, (await scope.Leaves.GetStudentBalanceAsync(seed.MehmetId, 2026)).AbsenceDays);
            await scope.Absences.RecordAsync(Absent(seed.MehmetId));   // gün yeniden kaydedilebilir
            Assert.Equal(2, (await scope.Absences.SearchAsync(new AbsenceQuery { Year = 2026 })).Count);   // eski kayıt silinmedi
        }
    }

    [Fact]
    public async Task Visibility_FollowsRoleScope()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await scope.Absences.RecordAsync(Absent(seed.MehmetId));
            await scope.Absences.RecordAsync(Absent(seed.ElifId));
            Assert.Equal(2, (await scope.Absences.SearchAsync(new AbsenceQuery())).Count);
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Equal("Mehmet Öztürk", Assert.Single(await scope.Absences.SearchAsync(new AbsenceQuery { DepartmentId = seed.YmId })).StudentName);

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
            Assert.Empty(await scope.Absences.SearchAsync(new AbsenceQuery()));

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
        {
            Assert.Equal("Elif Yıldız", Assert.Single(await scope.Absences.ListMineAsync(2026)).StudentName);
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Absences.SearchAsync(new AbsenceQuery()));
        }
    }

    [Fact]
    public async Task LeaveAllowance_CanBeSetPerStudent_OnlyByFaculty()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                scope.Students.SetLeaveAllowanceAsync(seed.MehmetId, new SetLeaveAllowanceRequest { Days = 15 }));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var detail = await scope.Students.SetLeaveAllowanceAsync(seed.MehmetId, new SetLeaveAllowanceRequest { Days = 15 });
            Assert.Equal(15, detail.LeaveDayAllowance);
            Assert.Equal(15, (await scope.Leaves.GetStudentBalanceAsync(seed.MehmetId, 2026)).Limit);
            Assert.Equal(10, (await scope.Leaves.GetStudentBalanceAsync(seed.ElifId, 2026)).Limit);   // diğer öğrenci etkilenmez

            var cleared = await scope.Students.SetLeaveAllowanceAsync(seed.MehmetId, new SetLeaveAllowanceRequest());
            Assert.Null(cleared.LeaveDayAllowance);
            Assert.Equal(10, (await scope.Leaves.GetStudentBalanceAsync(seed.MehmetId, 2026)).Limit);
        }

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LeaveAllowanceChanged));
    }

    [Fact]
    public async Task Balance_AbsencesAndLeaveShareTheSameAllowance()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        foreach (var day in new[] { 1, 2, 5, 6, 7, 8 })
            await scope.Absences.RecordAsync(Absent(seed.MehmetId, day));
        await scope.Absences.RecordAsync(Absent(seed.MehmetId, 9, AbsenceType.HealthReport));

        var balance = await scope.Leaves.GetStudentBalanceAsync(seed.MehmetId, 2026);

        Assert.Equal(7m, balance.AbsenceDays);          // tür fark etmez, hepsi düşer
        Assert.Equal(7m, balance.Used);
        Assert.Equal(3m, balance.Remaining);
    }
}
