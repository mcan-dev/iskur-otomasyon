using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

/// <summary>Mesai karekod saatleri ve monitör ekranı. Test saati: 21 Ekim 2026 Çarşamba, yerel 12:00.</summary>
public class AttendanceDisplayTests
{
    [Fact]
    public async Task Display_OutsideTheWindows_ShowsNoQrAndTheNextWindow()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        var display = await scope.AttendanceDisplay.GetDisplayAsync();

        Assert.Equal(DisplayWindow.Closed, display.Window);
        Assert.Null(display.Qr);
        Assert.Contains("16:30", display.NextWindow);
    }

    [Fact]
    public async Task Display_InsideTheWindowSetByTheFacultyOffice_ShowsAQrAndReusesTheSameSession()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
            await scope.AttendanceDisplay.UpdateScheduleAsync(new AttendanceScheduleDto
            {
                MorningStart = new TimeOnly(11, 30), MorningEnd = new TimeOnly(12, 30),
                EveningStart = new TimeOnly(16, 30), EveningEnd = new TimeOnly(17, 30)
            });

        int firstSession;
        using (var scope = db.NewScope())
        {
            var display = await scope.AttendanceDisplay.GetDisplayAsync();
            Assert.Equal(DisplayWindow.Morning, display.Window);
            Assert.NotNull(display.Qr);
            Assert.Equal(30, display.Qr!.WindowSeconds);
            firstSession = display.Qr.SessionId;
        }

        db.Clock.AdvanceSeconds(45);   // kod değişir, oturum aynı kalır
        using (var scope = db.NewScope())
        {
            var display = await scope.AttendanceDisplay.GetDisplayAsync();
            Assert.Equal(firstSession, display.Qr!.SessionId);
        }

        using var verify = db.NewContext();
        var session = await verify.AttendanceSessions.SingleAsync();
        Assert.Null(session.DepartmentId);                       // fakülte geneli
        Assert.Equal(new TimeOnly(11, 30), session.StartTime);
    }

    [Fact]
    public async Task Schedule_OnlyTheFacultyOfficeCanChangeIt_AndTimesMustBeConsistent()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.AttendanceDisplay.UpdateScheduleAsync(new AttendanceScheduleDto()));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var admin = db.NewScope();
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => admin.AttendanceDisplay.UpdateScheduleAsync(new AttendanceScheduleDto
        {
            MorningStart = new TimeOnly(9, 0), MorningEnd = new TimeOnly(8, 0),
            EveningStart = new TimeOnly(7, 0), EveningEnd = new TimeOnly(17, 0)
        }));
        Assert.True(ex.Errors.ContainsKey("MorningEnd"));
        Assert.True(ex.Errors.ContainsKey("EveningStart"));
    }

    [Fact]
    public async Task Display_OnAWeekend_IsClosed()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Clock.UtcNow = new DateTime(2026, 10, 24, 5, 30, 0, DateTimeKind.Utc);   // cumartesi 08:30
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        var display = await scope.AttendanceDisplay.GetDisplayAsync();
        Assert.Equal(DisplayWindow.Closed, display.Window);
    }
}

/// <summary>Kimin hangi gün çalıştığı ve geçmiş yıllarda çalışanlar.</summary>
public class WorkRosterTests
{
    private static void AddApprovedPlan(TestDb db, int studentId, int year, int month, params int[] days)
    {
        using var ctx = db.NewContext();
        var plan = new MonthlyWorkPlan { StudentProfileId = studentId, Year = year, Month = month, Status = WorkPlanStatus.Approved, LastSavedAt = db.Clock.UtcNow };
        foreach (var d in days) plan.Days.Add(new WorkPlanDay { Date = new DateOnly(year, month, d) });
        ctx.WorkPlans.Add(plan);
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Month_ListsWhoWorksEachDay_ForEveryResponsibleRole_WithinScope()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        AddApprovedPlan(db, seed.MehmetId, 2026, 11, 3, 5);
        AddApprovedPlan(db, seed.ElifId, 2026, 11, 3);
        TestData.AddLeaveDays(db, seed.MehmetId, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 5));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var roster = await scope.Roster.GetMonthAsync(new RosterQuery { Year = 2026, Month = 11 });
            Assert.Equal(2, roster.Days.Single(d => d.Date == new DateOnly(2026, 11, 3)).Students.Count);
            var onLeave = Assert.Single(roster.Days.Single(d => d.Date == new DateOnly(2026, 11, 5)).Students);
            Assert.Equal("OnLeave", onLeave.Status);
        }

        // Birim yetkilisi (BM) ve sorumlu personel yalnızca kendi öğrencilerini görür.
        foreach (var (role, user) in new[] { (UserRole.DepartmentHead, seed.HeadId), (UserRole.Advisor, seed.AdvisorId) })
        {
            db.Current.As(role, user, seed.BmId);
            using var scope = db.NewScope();
            var roster = await scope.Roster.GetMonthAsync(new RosterQuery { Year = 2026, Month = 11 });
            Assert.All(roster.Days.SelectMany(d => d.Students), s => Assert.Equal(seed.MehmetId, s.StudentId));
        }
    }

    [Fact]
    public async Task Year_ShowsPastYearsWorkers_ForHeadsAndFacultyOnly()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        AddApprovedPlan(db, seed.MehmetId, 2025, 11, 4, 6, 11);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var workers = await scope.Roster.GetYearAsync(new YearWorkersQuery { Year = 2025 });
            var row = Assert.Single(workers.Students);
            Assert.Equal(seed.MehmetId, row.StudentId);
            Assert.Equal(3, row.PlannedDays);
            Assert.Empty((await scope.Roster.GetYearAsync(new YearWorkersQuery { Year = 2024 })).Students);
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Single((await scope.Roster.GetYearAsync(new YearWorkersQuery { Year = 2025 })).Students);

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Roster.GetYearAsync(new YearWorkersQuery { Year = 2025 }));
    }
}

/// <summary>Üniversite hayatı boyunca en fazla 140 iş günü.</summary>
public class LifetimeWorkDaysTests
{
    private static SaveWorkPlanRequest NovemberPlan(params int[] days) => new()
    {
        Year = 2026, Month = 11,
        Days = days.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 11, d) }).ToList()
    };

    [Fact]
    public async Task Lifetime_CountsWorkedDays_AndIsVisibleToStudentAndStaff()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
        {
            var mine = await scope.WorkPlans.GetMyLifetimeAsync();
            Assert.Equal(140, mine.Limit);
            Assert.Equal(0, mine.WorkedDays);
            Assert.Equal(140, mine.RemainingDays);
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Equal(140, (await scope.WorkPlans.GetStudentLifetimeAsync(seed.MehmetId)).RemainingDays);

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.WorkPlans.GetStudentLifetimeAsync(seed.ElifId));
    }

    [Fact]
    public async Task SavePlan_BeyondTheLifetimeLimit_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.WorkPlanSettings.LifetimeMaxDays = 3;
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using (var scope = db.NewScope())
        {
            var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.WorkPlans.SaveMyPlanAsync(NovemberPlan(3, 10, 17, 24)));
            Assert.Contains("en fazla 3 iş günü", string.Join(" ", ex.Errors["Days"]));
        }

        db.WorkPlanSettings.LifetimeMaxDays = 4;
        using (var scope = db.NewScope())
        {
            await scope.WorkPlans.SaveMyPlanAsync(NovemberPlan(3, 10, 17, 24));
            var lifetime = await scope.WorkPlans.GetMyLifetimeAsync();
            Assert.Equal(4, lifetime.UpcomingPlannedDays);
        }
    }
}
