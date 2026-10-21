using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.Attendance;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

/// <summary>
/// Saat sabit: yerel saat 12:00, tarih 21 Ekim 2026 (Çarşamba).
/// Mehmet'in Ekim planı 6, 14, 21 ve 28. günlerdir ve onaylıdır.
/// </summary>
public class AttendanceServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    private sealed class Fixture : IDisposable
    {
        public required TestDb Db { get; init; }
        public required SeedResult Seed { get; init; }
        public void Dispose() => Db.Dispose();
    }

    private static async Task<Fixture> CreateAsync(bool approvePlan = true)
    {
        var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();
        var plan = await scope.WorkPlans.SaveForStudentAsync(seed.MehmetId, new SaveWorkPlanRequest
        {
            Year = 2026,
            Month = 10,
            Days = new[] { 6, 14, 21, 28 }.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 10, d) }).ToList()
        });
        if (approvePlan)
            await scope.WorkPlans.ReviewAsync(plan.Id, new ReviewWorkPlanRequest { Approve = true });

        return new Fixture { Db = db, Seed = seed };
    }

    private static CreateSessionRequest Session(int startHour = 11, int endHour = 14, int? departmentId = null) => new()
    {
        Date = Today,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0),
        DepartmentId = departmentId
    };

    private static async Task<SessionDto> OpenSessionAsync(Fixture f, CreateSessionRequest? request = null)
    {
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();
        return await scope.Attendance.CreateSessionAsync(request ?? Session());
    }

    private static async Task<string> CurrentTokenAsync(Fixture f, int sessionId)
    {
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();
        return (await scope.Attendance.GetCurrentQrAsync(sessionId)).Token;
    }

    private static async Task<AttendanceDto> CheckInAsync(Fixture f, int userId, string token)
    {
        f.Db.Current.As(UserRole.Student, userId);
        using var scope = f.Db.NewScope();
        return await scope.Attendance.CheckInAsync(new CheckInRequest { Token = token });
    }

    // ─── Oturum ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_ByAdmin_IsActiveInsideItsWindow()
    {
        using var f = await CreateAsync();

        var session = await OpenSessionAsync(f);

        Assert.True(session.IsActiveNow);
        Assert.False(session.IsClosed);
        Assert.Null(session.DepartmentId);
    }

    [Fact]
    public async Task CreateSession_AsDepartmentHead_IsLimitedToTheirOwnDepartment()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.DepartmentHead, f.Seed.HeadId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        var session = await scope.Attendance.CreateSessionAsync(Session(departmentId: f.Seed.YmId));

        Assert.Equal(f.Seed.BmId, session.DepartmentId);
    }

    [Theory]
    [InlineData(UserRole.Student)]
    [InlineData(UserRole.Advisor)]
    public async Task CreateSession_ByOtherRoles_IsForbidden(UserRole role)
    {
        using var f = await CreateAsync();
        f.Db.Current.As(role, f.Seed.AdvisorId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Attendance.CreateSessionAsync(Session()));
    }

    [Fact]
    public async Task CreateSession_WithBadTimes_ReportsEveryProblem()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();

        var past = Session();
        past.Date = Today.AddDays(-1);
        var pastError = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Attendance.CreateSessionAsync(past));
        Assert.True(pastError.Errors.ContainsKey("Date"));

        var reversed = Session(startHour: 14, endHour: 11);
        var reversedError = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Attendance.CreateSessionAsync(reversed));
        Assert.True(reversedError.Errors.ContainsKey("EndTime"));

        var finished = Session(startHour: 8, endHour: 10);   // bugün, saat 12:00
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Attendance.CreateSessionAsync(finished));
    }

    [Fact]
    public async Task CreateSession_OverlappingAnOpenOne_IsRejected_ButClosedOnesDoNotBlock()
    {
        using var f = await CreateAsync();
        var first = await OpenSessionAsync(f, Session(11, 14));

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
        {
            await Assert.ThrowsAsync<ConflictException>(() => scope.Attendance.CreateSessionAsync(Session(13, 16)));
            await scope.Attendance.CloseSessionAsync(first.Id);
        }

        using (var scope = f.Db.NewScope())
            await scope.Attendance.CreateSessionAsync(Session(13, 16));
    }

    [Fact]
    public async Task SessionsOfDifferentDepartments_MayRunAtTheSameTime()
    {
        using var f = await CreateAsync();
        await OpenSessionAsync(f, Session(departmentId: f.Seed.BmId));

        await OpenSessionAsync(f, Session(departmentId: f.Seed.YmId));
    }

    [Fact]
    public async Task DepartmentHead_CannotSeeOrUseAnotherDepartmentsSession()
    {
        using var f = await CreateAsync();
        var ymSession = await OpenSessionAsync(f, Session(departmentId: f.Seed.YmId));

        f.Db.Current.As(UserRole.DepartmentHead, f.Seed.HeadId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<NotFoundException>(() => scope.Attendance.GetCurrentQrAsync(ymSession.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => scope.Attendance.CloseSessionAsync(ymSession.Id));
        Assert.Empty(await scope.Attendance.ListSessionsAsync(null, null));
    }

    [Fact]
    public async Task Qr_IsOnlyIssuedInsideTheSessionWindow_AndNeverForClosedSessions()
    {
        using var f = await CreateAsync();
        var later = await OpenSessionAsync(f, Session(15, 17));

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Attendance.GetCurrentQrAsync(later.Id));

        var running = await scope.Attendance.CreateSessionAsync(Session(11, 14));
        var qr = await scope.Attendance.GetCurrentQrAsync(running.Id);
        Assert.Equal(QrToken.WindowSeconds, qr.WindowSeconds);
        Assert.True(qr.RefreshAtUtc > f.Db.Clock.UtcNow);
        Assert.True(qr.RefreshAtUtc <= f.Db.Clock.UtcNow.AddSeconds(QrToken.WindowSeconds));

        await scope.Attendance.CloseSessionAsync(running.Id);
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Attendance.GetCurrentQrAsync(running.Id));
        await Assert.ThrowsAsync<ConflictException>(() => scope.Attendance.CloseSessionAsync(running.Id));
    }

    [Fact]
    public async Task TheQrChangesFromOneWindowToTheNext()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);

        var before = await CurrentTokenAsync(f, session.Id);
        f.Db.Clock.AdvanceSeconds(31);
        var after = await CurrentTokenAsync(f, session.Id);

        Assert.NotEqual(before, after);
    }

    // ─── Okutma ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckIn_WithValidQr_RecordsAttendanceLinkedToThePlanDay()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        var record = await CheckInAsync(f, f.Seed.MehmetUserId, token);

        Assert.Equal(Today, record.Date);
        Assert.Equal("Mehmet Öztürk", record.StudentName);
        Assert.False(record.IsManual);
        Assert.Equal(f.Db.Clock.UtcNow, record.CheckedInAt);

        using var verify = f.Db.NewContext();
        var saved = await verify.AttendanceRecords.Include(r => r.WorkPlanDay).SingleAsync();
        Assert.Equal(Today, saved.WorkPlanDay!.Date);
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.CheckInSucceeded));
    }

    [Fact]
    public async Task CheckIn_TwiceForTheSameSession_IsRejected()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);
        await CheckInAsync(f, f.Seed.MehmetUserId, token);

        await Assert.ThrowsAsync<ConflictException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));

        using var verify = f.Db.NewContext();
        Assert.Equal(1, await verify.AttendanceRecords.CountAsync());
    }

    [Fact]
    public async Task CheckIn_WithAnExpiredToken_IsRejectedAndLogged()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        f.Db.Clock.AdvanceSeconds(75);   // iki pencere geçti; öğrenciye iletilen eski karekod

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));

        using var verify = f.Db.NewContext();
        Assert.False(await verify.AttendanceRecords.AnyAsync());
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.CheckInRejected));
    }

    [Theory]
    [InlineData("")]
    [InlineData("rastgele-metin")]
    [InlineData("999.1.imza")]
    public async Task CheckIn_WithGarbageToken_IsRejected(string token)
    {
        using var f = await CreateAsync();
        await OpenSessionAsync(f);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_WithATamperedSignature_IsRejected()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);
        var forged = token[..^2] + (token.EndsWith("AA") ? "BB" : "AA");

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, forged));
    }

    [Fact]
    public async Task CheckIn_AfterTheSessionEnds_IsRejected_EvenWithAStillValidToken()
    {
        using var f = await CreateAsync();
        var request = Session();
        request.StartTime = new TimeOnly(11, 0);
        request.EndTime = new TimeOnly(12, 0, 50);
        var session = await OpenSessionAsync(f, request);
        var token = await CurrentTokenAsync(f, session.Id);

        f.Db.Clock.AdvanceSeconds(55);   // 12:00:55: jeton hâlâ tolerans içinde ama oturum bitti

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_ToAClosedSession_IsRejected()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
            await scope.Attendance.CloseSessionAsync(session.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_WithoutAnApprovedPlanForTheDay_IsRejected()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        // Elif'in hiç planı yok.
        var error = await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.ElifUserId, token));
        Assert.Contains("plan", error.Message);
    }

    [Fact]
    public async Task CheckIn_WhenThePlanIsStillPending_IsRejected()
    {
        using var f = await CreateAsync(approvePlan: false);
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_OnADayThatIsNotInThePlan_IsRejected()
    {
        using var f = await CreateAsync();
        f.Db.Clock.AdvanceDays(1);   // 22 Ekim planda yok
        var tomorrow = Session();
        tomorrow.Date = Today.AddDays(1);
        var session = await OpenSessionAsync(f, tomorrow);
        var token = await CurrentTokenAsync(f, session.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_OnAnApprovedFullDayLeave_IsRejected()
    {
        using var f = await CreateAsync();

        f.Db.Current.As(UserRole.Student, f.Seed.MehmetUserId);
        LeaveResultDto leave;
        using (var scope = f.Db.NewScope())
            leave = await scope.Leaves.RequestAsync(new CreateLeaveRequest
            {
                Type = LeaveType.Excuse, StartDate = Today, EndDate = Today, Reason = "Aile işi"
            });
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
            await scope.Leaves.ReviewAsync(leave.Leave.Id, new ReviewLeaveRequest { Approve = true });

        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_ToADepartmentSession_RequiresThatDepartment()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f, Session(departmentId: f.Seed.YmId));
        var token = await CurrentTokenAsync(f, session.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task CheckIn_AfterLeavingTheProgram_IsRejected()
    {
        using var f = await CreateAsync();
        using (var ctx = f.Db.NewContext())
        {
            var participation = await ctx.ProgramParticipations.SingleAsync(p => p.StudentProfileId == f.Seed.MehmetId);
            participation.EndDate = Today.AddDays(-1);
            await ctx.SaveChangesAsync();
        }
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() => CheckInAsync(f, f.Seed.MehmetUserId, token));
    }

    [Fact]
    public async Task OnlyStudentsCanCheckIn()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Attendance.CheckInAsync(new CheckInRequest { Token = token }));
    }

    // ─── Düzeltme ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddManually_NeedsAReason_AndRecordsWhoDidIt()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Attendance.AddManuallyAsync(new ManualAttendanceRequest { StudentId = f.Seed.MehmetId, SessionId = session.Id, Reason = " " }));

        var record = await scope.Attendance.AddManuallyAsync(new ManualAttendanceRequest
        {
            StudentId = f.Seed.MehmetId, SessionId = session.Id, Reason = "Telefonu bozuktu"
        });

        Assert.True(record.IsManual);
        Assert.Equal("Telefonu bozuktu", record.ManualReason);
        Assert.Equal("Ayşe Kaya", record.CreatedByName);
        await Assert.ThrowsAsync<ConflictException>(() =>
            scope.Attendance.AddManuallyAsync(new ManualAttendanceRequest { StudentId = f.Seed.MehmetId, SessionId = session.Id, Reason = "Tekrar" }));
    }

    [Fact]
    public async Task AddManually_IsOnlyForFacultyAdmin()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);

        f.Db.Current.As(UserRole.DepartmentHead, f.Seed.HeadId, f.Seed.BmId);
        using var scope = f.Db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            scope.Attendance.AddManuallyAsync(new ManualAttendanceRequest { StudentId = f.Seed.MehmetId, SessionId = session.Id, Reason = "Deneme" }));
    }

    [Fact]
    public async Task Void_KeepsTheRecord_AndFreesTheSessionForANewOne()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        var token = await CurrentTokenAsync(f, session.Id);
        var record = await CheckInAsync(f, f.Seed.MehmetUserId, token);

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
        {
            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.Attendance.VoidAsync(record.Id, new VoidAttendanceRequest { Reason = "" }));

            var voided = await scope.Attendance.VoidAsync(record.Id, new VoidAttendanceRequest { Reason = "Yanlış kişi okuttu" });
            Assert.True(voided.IsVoided);
            Assert.Equal("Ayşe Kaya", voided.VoidedByName);
            Assert.Equal("Yanlış kişi okuttu", voided.VoidReason);

            await Assert.ThrowsAsync<ConflictException>(() =>
                scope.Attendance.VoidAsync(record.Id, new VoidAttendanceRequest { Reason = "Tekrar" }));

            Assert.Empty(await scope.Attendance.SearchAsync(new AttendanceQuery { Date = Today }));
            Assert.Single(await scope.Attendance.SearchAsync(new AttendanceQuery { Date = Today, IncludeVoided = true }));
        }

        await CheckInAsync(f, f.Seed.MehmetUserId, token);   // iptalden sonra yeniden okutabilir

        using var verify = f.Db.NewContext();
        Assert.Equal(2, await verify.AttendanceRecords.CountAsync());
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.AttendanceVoided));
    }

    // ─── Listeleme ve kapsam ─────────────────────────────────────────────────

    [Fact]
    public async Task Search_RespectsTheRoleScope()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
        {
            await scope.Attendance.AddManuallyAsync(new ManualAttendanceRequest { StudentId = f.Seed.MehmetId, SessionId = session.Id, Reason = "Elle" });
            await scope.Attendance.AddManuallyAsync(new ManualAttendanceRequest { StudentId = f.Seed.ElifId, SessionId = session.Id, Reason = "Elle" });
            Assert.Equal(2, (await scope.Attendance.SearchAsync(new AttendanceQuery { Date = Today })).Count);
        }

        f.Db.Current.As(UserRole.DepartmentHead, f.Seed.HeadId, f.Seed.BmId);
        using (var scope = f.Db.NewScope())
        {
            var visible = await scope.Attendance.SearchAsync(new AttendanceQuery { Date = Today, DepartmentId = f.Seed.YmId });
            Assert.Equal("Mehmet Öztürk", Assert.Single(visible).StudentName);   // istenen bölüm filtresi kapsamı aşamaz
        }

        f.Db.Current.As(UserRole.Advisor, f.Seed.AdvisorId, f.Seed.BmId);
        using (var scope = f.Db.NewScope())
            Assert.Single(await scope.Attendance.SearchAsync(new AttendanceQuery { Year = 2026, Month = 10 }));

        f.Db.Current.As(UserRole.Student, f.Seed.MehmetUserId);
        using (var scope = f.Db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Attendance.SearchAsync(new AttendanceQuery()));
    }

    [Fact]
    public async Task Student_SeesOnlyOwnRecords_ByMonthAndYear()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        await CheckInAsync(f, f.Seed.MehmetUserId, await CurrentTokenAsync(f, session.Id));

        f.Db.Current.As(UserRole.Student, f.Seed.MehmetUserId);
        using var scope = f.Db.NewScope();
        Assert.Single(await scope.Attendance.ListMineAsync(2026, 10));
        Assert.Single(await scope.Attendance.ListMineAsync(2026, null));
        Assert.Empty(await scope.Attendance.ListMineAsync(2026, 11));

        f.Db.Current.As(UserRole.Student, f.Seed.ElifUserId);
        using var other = f.Db.NewScope();
        Assert.Empty(await other.Attendance.ListMineAsync(null, null));
    }

    // ─── Planlanan - gerçekleşen ─────────────────────────────────────────────

    [Fact]
    public async Task Comparison_ShowsAttendedMissingAndUpcomingPlannedDays()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);
        await CheckInAsync(f, f.Seed.MehmetUserId, await CurrentTokenAsync(f, session.Id));

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
            await scope.Absences.RecordAsync(new RecordAbsenceRequest
            {
                StudentId = f.Seed.MehmetId, Date = new DateOnly(2026, 10, 6), Type = AbsenceType.Unexcused
            });

        f.Db.Current.As(UserRole.Student, f.Seed.MehmetUserId);
        using var mine = f.Db.NewScope();
        var comparison = await mine.Attendance.GetMyComparisonAsync(2026, 10);

        Assert.True(comparison.HasApprovedPlan);
        Assert.Equal(4m, comparison.PlannedDays);
        Assert.Equal(1m, comparison.AttendedDays);          // 21 Ekim
        Assert.Equal(1m, comparison.RecordedAbsenceDays);   // 6 Ekim
        Assert.Equal(1m, comparison.MissingDays);           // 14 Ekim: kayıt da izin de yok
        Assert.Equal(PlannedDayOutcome.Upcoming, comparison.Days.Single(d => d.Date.Day == 28).Outcome);
    }

    [Fact]
    public async Task Comparison_WithoutAnApprovedPlan_IsEmpty()
    {
        using var f = await CreateAsync(approvePlan: false);
        f.Db.Current.As(UserRole.Advisor, f.Seed.AdvisorId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        var comparison = await scope.Attendance.GetComparisonAsync(f.Seed.MehmetId, 2026, 10);

        Assert.False(comparison.HasApprovedPlan);
        Assert.Empty(comparison.Days);
    }

    [Fact]
    public async Task Comparison_OfAStudentOutsideTheScope_LooksLikeNotFound()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.Advisor, f.Seed.AdvisorYmId, f.Seed.YmId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<NotFoundException>(() => scope.Attendance.GetComparisonAsync(f.Seed.MehmetId, 2026, 10));
    }

    // ─── Veritabanı güvencesi ────────────────────────────────────────────────

    [Fact]
    public async Task Database_RejectsTwoActiveRecordsForTheSameStudentAndSession()
    {
        using var f = await CreateAsync();
        var session = await OpenSessionAsync(f);

        using var ctx = f.Db.NewContext();
        AttendanceRecord NewRecord() => new()
        {
            StudentProfileId = f.Seed.MehmetId,
            AttendanceSessionId = session.Id,
            Date = Today,
            CheckedInAt = f.Db.Clock.UtcNow
        };
        ctx.AttendanceRecords.Add(NewRecord());
        await ctx.SaveChangesAsync();

        ctx.AttendanceRecords.Add(NewRecord());
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }
}
