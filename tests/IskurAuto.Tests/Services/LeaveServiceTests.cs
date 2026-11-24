using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

/// <summary>
/// İzin: öğrenci onaylı plandaki çalışma günlerinden seçer; tür ve gerekçe yoktur; izin onay beklemeden geçerlidir.
/// Ayda en az bir gün çalışılmalıdır. Sekreterlik izni onaylamaz, gerekirse iptal eder ve hakkı ayarlar.
/// Test saati: 21 Ekim 2026 Çarşamba.
/// </summary>
public class LeaveServiceTests
{
    private static DateOnly Nov(int day) => new(2026, 11, day);

    private static CreateLeaveRequest Leave(params int[] novemberDays) => new()
    {
        Days = novemberDays.Select(Nov).ToList()
    };

    /// <summary>Öğrenciye onaylı (veya verilen durumda) bir aylık plan ekler.</summary>
    private static void AddPlan(TestDb db, int studentId, int year, int month, WorkPlanStatus status, params int[] days)
    {
        using var ctx = db.NewContext();
        var plan = new MonthlyWorkPlan { StudentProfileId = studentId, Year = year, Month = month, Status = status, LastSavedAt = db.Clock.UtcNow };
        foreach (var d in days)
            plan.Days.Add(new WorkPlanDay { Date = new DateOnly(year, month, d) });
        ctx.WorkPlans.Add(plan);
        ctx.SaveChanges();
    }

    /// <summary>Mehmet için Kasım 2026'da 6 çalışma günü olan onaylı plan; öğrenci olarak oturum açılmış.</summary>
    private static (TestDb Db, SeedResult Seed) StudentWithNovemberPlan()
    {
        var db = new TestDb();
        var seed = TestData.Seed(db);
        AddPlan(db, seed.MehmetId, 2026, 11, WorkPlanStatus.Approved, 3, 5, 10, 12, 17, 19);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        return (db, seed);
    }

    // ─── Takvim ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Calendar_OnlyFutureWorkDaysOfTheApprovedPlanAreSelectable()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        AddPlan(db, seed.MehmetId, 2026, 10, WorkPlanStatus.Approved, 20, 22, 27);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using var scope = db.NewScope();
        var calendar = await scope.Leaves.GetMyCalendarAsync(2026, 10);

        Assert.True(calendar.HasApprovedPlan);
        Assert.Equal(31, calendar.Days.Count);
        var selectable = calendar.Days.Where(d => d.CanSelect).Select(d => d.Date.Day).ToList();
        Assert.Equal(new[] { 22, 27 }, selectable);                          // 20 Ekim geçti
        Assert.True(calendar.Days.Single(d => d.Date.Day == 20).IsWorkDay);
        Assert.False(calendar.Days.Single(d => d.Date.Day == 23).IsWorkDay);  // planda olmayan gün
        Assert.Equal(10m, calendar.Balance.Remaining);
    }

    [Fact]
    public async Task Calendar_WithoutApprovedPlan_HasNothingToSelect()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        AddPlan(db, seed.MehmetId, 2026, 11, WorkPlanStatus.Pending, 3, 5);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using var scope = db.NewScope();
        var calendar = await scope.Leaves.GetMyCalendarAsync(2026, 11);

        Assert.False(calendar.HasApprovedPlan);
        Assert.DoesNotContain(calendar.Days, d => d.CanSelect);
    }

    // ─── İzin alma ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Request_CreatesOneValidLeavePerSelectedDay_WithoutApproval()
    {
        var (db, _) = StudentWithNovemberPlan();
        using var _ = db;

        using (var scope = db.NewScope())
        {
            var created = await scope.Leaves.RequestAsync(new CreateLeaveRequest { Days = new() { Nov(10), Nov(3) }, Description = "  Aile ziyareti " });

            Assert.Equal(2, created.Count);
            Assert.All(created, l => Assert.Equal(LeaveStatus.Approved, l.Status));
            Assert.All(created, l => Assert.Equal(1m, l.DaysCounted));
            Assert.Equal(new[] { Nov(3), Nov(10) }, created.Select(l => l.StartDate));
            Assert.All(created, l => Assert.Equal("Aile ziyareti", l.Description));
            Assert.All(created, l => Assert.Equal(new DateOnly(2026, 10, 21), l.AppliedOn));

            var balance = await scope.Leaves.GetMyBalanceAsync(2026);
            Assert.Equal(2m, balance.LeaveDays);
            Assert.Equal(8m, balance.Remaining);
        }

        using var verify = db.NewContext();
        Assert.Equal(2, await verify.AuditLogs.CountAsync(a => a.Action == AuditActions.LeaveRequested));
    }

    [Fact]
    public async Task Request_DescriptionIsOptional()
    {
        var (db, _) = StudentWithNovemberPlan();
        using var _ = db;
        using var scope = db.NewScope();

        var created = await scope.Leaves.RequestAsync(Leave(5));
        Assert.Null(Assert.Single(created).Description);
    }

    [Fact]
    public async Task Request_ForADayThatIsNotAWorkDay_IsRejected()
    {
        var (db, _) = StudentWithNovemberPlan();
        using var _ = db;
        using var scope = db.NewScope();

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.RequestAsync(Leave(4)));   // 4 Kasım planda yok
        Assert.Contains("çalışma gününüz değil", string.Join(" ", ex.Errors["Days"]));
    }

    [Fact]
    public async Task Request_ForAPastDay_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        AddPlan(db, seed.MehmetId, 2026, 10, WorkPlanStatus.Approved, 20, 22, 27);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using var scope = db.NewScope();
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Leaves.RequestAsync(new CreateLeaveRequest { Days = new() { new DateOnly(2026, 10, 20) } }));
        Assert.Contains("geçmiş", string.Join(" ", ex.Errors["Days"]));
    }

    [Fact]
    public async Task Request_WithoutAnApprovedPlan_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        AddPlan(db, seed.MehmetId, 2026, 11, WorkPlanStatus.Pending, 3, 5, 10);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.RequestAsync(Leave(3)));
    }

    [Fact]
    public async Task Request_CannotCoverEveryWorkDayOfTheMonth()
    {
        var (db, _) = StudentWithNovemberPlan();
        using var _ = db;

        using (var scope = db.NewScope())
        {
            var ex = await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.RequestAsync(Leave(3, 5, 10, 12, 17, 19)));
            Assert.Contains("en az bir gün", ex.Message);
        }

        // Parça parça alınarak da ayın tamamı kapatılamaz.
        using (var scope = db.NewScope())
            await scope.Leaves.RequestAsync(Leave(3, 5));
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.RequestAsync(Leave(10, 12, 17, 19)));
    }

    [Fact]
    public async Task Request_DaysFromTwoMonths_AreRejected()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;
        AddPlan(db, seed.MehmetId, 2026, 12, WorkPlanStatus.Approved, 1, 3, 8);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Leaves.RequestAsync(new CreateLeaveRequest { Days = new() { Nov(3), new DateOnly(2026, 12, 1) } }));
    }

    [Fact]
    public async Task Request_TheSameDayTwice_IsRejected()
    {
        var (db, _) = StudentWithNovemberPlan();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.Leaves.RequestAsync(Leave(3));
        using (var scope = db.NewScope())
        {
            var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Leaves.RequestAsync(Leave(3)));
            Assert.Contains("zaten izniniz var", string.Join(" ", ex.Errors["Days"]));
        }
    }

    [Fact]
    public async Task Request_BeyondTheRemainingAllowance_IsRejected()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Students.SetLeaveAllowanceAsync(seed.MehmetId, new SetLeaveAllowanceRequest { Days = 1 });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using var student = db.NewScope();
        await Assert.ThrowsAsync<ConflictException>(() => student.Leaves.RequestAsync(Leave(3, 5)));
        await student.Leaves.RequestAsync(Leave(3));
    }

    [Fact]
    public async Task Request_ByInactiveStudent_IsRejected()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;
        using (var ctx = db.NewContext())
        {
            var open = await ctx.ProgramParticipations.SingleAsync(p => p.StudentProfileId == seed.MehmetId);
            open.EndDate = db.Clock.Today;
            await ctx.SaveChangesAsync();
        }

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.RequestAsync(Leave(3)));
    }

    // ─── İptal ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Student_CanCancelOwnFutureLeave_AndTheDayReturnsToTheAllowance()
    {
        var (db, _) = StudentWithNovemberPlan();
        using var _ = db;

        int id;
        using (var scope = db.NewScope())
            id = (await scope.Leaves.RequestAsync(Leave(3))).Single().Id;

        using (var scope = db.NewScope())
        {
            var cancelled = await scope.Leaves.CancelMineAsync(id);
            Assert.Equal(LeaveStatus.Cancelled, cancelled.Status);
            Assert.Equal(10m, (await scope.Leaves.GetMyBalanceAsync(2026)).Remaining);
        }
    }

    [Fact]
    public async Task Student_CannotCancelSomeoneElsesLeave()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        int id;
        using (var scope = db.NewScope())
            id = (await scope.Leaves.RequestAsync(Leave(3))).Single().Id;

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using var other = db.NewScope();
        await Assert.ThrowsAsync<NotFoundException>(() => other.Leaves.CancelMineAsync(id));
    }

    [Fact]
    public async Task FacultyOffice_CanCancelALeave_WithANote()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        int id;
        using (var scope = db.NewScope())
            id = (await scope.Leaves.RequestAsync(Leave(3))).Single().Id;

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var cancelled = await scope.Leaves.CancelAsync(id, new CancelLeaveRequest { Note = "Öğrenci o gün çalıştı." });
            Assert.Equal(LeaveStatus.Cancelled, cancelled.Status);
            Assert.Equal("Ayşe Kaya", cancelled.CancelledByName);
            Assert.Equal("Öğrenci o gün çalıştı.", cancelled.CancelNote);

            await Assert.ThrowsAsync<ConflictException>(() => scope.Leaves.CancelAsync(id, new CancelLeaveRequest()));
        }

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LeaveCancelled && a.EntityId == id));
    }

    [Theory]
    [InlineData(UserRole.DepartmentHead)]
    [InlineData(UserRole.Advisor)]
    public async Task OnlyTheFacultyOffice_CanCancelLeaves(UserRole role)
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        int id;
        using (var scope = db.NewScope())
            id = (await scope.Leaves.RequestAsync(Leave(3))).Single().Id;

        db.Current.As(role, role == UserRole.Advisor ? seed.AdvisorId : seed.HeadId, seed.BmId);
        using var staff = db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() => staff.Leaves.CancelAsync(id, new CancelLeaveRequest()));
    }

    // ─── Görünürlük: öğrencinin sorumluları görür ─────────────────────────────

    [Fact]
    public async Task Leaves_AreVisibleToEveryResponsiblePerson_ButNotToOthers()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        int id;
        using (var scope = db.NewScope())
            id = (await scope.Leaves.RequestAsync(Leave(3))).Single().Id;

        // Sekreterlik, birim yetkilisi (aynı bölüm) ve sorumlu personel görür.
        foreach (var (role, user) in new[] { (UserRole.FacultyAdmin, seed.AdminId), (UserRole.DepartmentHead, seed.HeadId), (UserRole.Advisor, seed.AdvisorId) })
        {
            db.Current.As(role, user, role == UserRole.FacultyAdmin ? null : seed.BmId);
            using var scope = db.NewScope();
            Assert.Contains(await scope.Leaves.SearchAsync(new LeaveSearchQuery { Year = 2026 }), l => l.Id == id);
            Assert.Equal(id, (await scope.Leaves.GetByIdAsync(id)).Id);
        }

        // Başka bölümün sorumlu personeli göremez.
        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
        {
            Assert.DoesNotContain(await scope.Leaves.SearchAsync(new LeaveSearchQuery { Year = 2026 }), l => l.Id == id);
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.GetByIdAsync(id));
        }

        // Diğer öğrenciler göremez.
        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Leaves.SearchAsync(new LeaveSearchQuery { Year = 2026 }));
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Leaves.GetByIdAsync(id));
        }
    }

    [Fact]
    public async Task Search_CanFilterByMonth()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;
        using (var scope = db.NewScope())
            await scope.Leaves.RequestAsync(Leave(3));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var staff = db.NewScope();
        Assert.Single(await staff.Leaves.SearchAsync(new LeaveSearchQuery { Year = 2026, Month = 11 }));
        Assert.Empty(await staff.Leaves.SearchAsync(new LeaveSearchQuery { Year = 2026, Month = 12 }));
    }

    // ─── Hak ve uyarılar ──────────────────────────────────────────────────────

    [Fact]
    public async Task Balance_CountsLeavesAndAbsences_ButNotCancelledLeaves()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        int cancelledId;
        using (var scope = db.NewScope())
        {
            await scope.Leaves.RequestAsync(Leave(3));
            cancelledId = (await scope.Leaves.RequestAsync(Leave(5))).Single().Id;
            await scope.Leaves.CancelMineAsync(cancelledId);
        }

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Absences.RecordAsync(new RecordAbsenceRequest { StudentId = seed.MehmetId, Date = new DateOnly(2026, 10, 19) });

        using (var scope = db.NewScope())
        {
            var balance = await scope.Leaves.GetStudentBalanceAsync(seed.MehmetId, 2026);
            Assert.Equal(1m, balance.LeaveDays);
            Assert.Equal(1m, balance.AbsenceDays);
            Assert.Equal(8m, balance.Remaining);
        }
    }

    [Fact]
    public async Task Alerts_ListStudentsNearTheLimit()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Students.SetLeaveAllowanceAsync(seed.MehmetId, new SetLeaveAllowanceRequest { Days = 3 });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await scope.Leaves.RequestAsync(Leave(3, 5));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var staff = db.NewScope();
        var alert = Assert.Single(await staff.Leaves.GetAlertsAsync(2026));
        Assert.Equal(seed.MehmetId, alert.StudentId);
        Assert.Equal(1m, alert.Balance.Remaining);
        Assert.Contains(alert.Balance.Warnings, w => w.Code == "LeaveNearLimit");
    }

    [Fact]
    public async Task OnlyTheFacultyOffice_CanChangeTheStudentsLeaveAllowance()
    {
        var (db, seed) = StudentWithNovemberPlan();
        using var _ = db;

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
        }
    }
}
