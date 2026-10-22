using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class WorkPlanServiceTests
{
    // Testlerde saat 21 Ekim 2026. Kasım planı 1 Ekim'de açılır, 5 Kasım'da kapanır.
    private static SaveWorkPlanRequest NovemberPlan(params int[] days) => new()
    {
        Year = 2026,
        Month = 11,
        Days = days.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 11, d) }).ToList()
    };

    private static SaveWorkPlanRequest ValidPlan() => NovemberPlan(3, 10, 17, 24);

    private static (TestDb Db, SeedResult Seed) StudentSession()
    {
        var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        return (db, seed);
    }

    [Fact]
    public async Task SaveMyPlan_WithValidDays_StoresPendingPlanWithHours()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        using var scope = db.NewScope();

        var plan = await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        Assert.Equal(WorkPlanStatus.Pending, plan.Status);
        Assert.Equal(4, plan.PlannedDays);
        Assert.Equal(32m, plan.PlannedHours);
    }

    [Fact]
    public async Task SaveMyPlan_EveryPlannedDayIsAnEightHourDay()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        using var scope = db.NewScope();

        var plan = await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        Assert.All(plan.Days, d => Assert.Equal(8m, d.Hours));
        Assert.Equal(plan.PlannedDays * 8m, plan.PlannedHours);
    }

    [Fact]
    public async Task SaveMyPlan_Twice_UpdatesSamePlanAndRemovesDroppedDays()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());
        using (var scope = db.NewScope())
            await scope.WorkPlans.SaveMyPlanAsync(NovemberPlan(4, 10, 17, 24));

        using var verify = db.NewContext();
        var plan = await verify.WorkPlans.Include(p => p.Days).SingleAsync();
        Assert.Equal(4, plan.Days.Count);
        Assert.DoesNotContain(plan.Days, d => d.Date == new DateOnly(2026, 11, 3));
        Assert.Contains(plan.Days, d => d.Date == new DateOnly(2026, 11, 4));
    }

    [Fact]
    public async Task SaveMyPlan_WithRuleViolations_ThrowsValidationAndStoresNothing()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
        {
            var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.WorkPlans.SaveMyPlanAsync(NovemberPlan(3, 10, 17)));
            Assert.True(ex.Errors.ContainsKey("Days"));
        }

        using var verify = db.NewContext();
        Assert.False(await verify.WorkPlans.AnyAsync());
    }

    [Fact]
    public async Task SaveMyPlan_LessonDayNeedsNote_AfterCourseDaysAreDeclared()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.CourseDays.SetMineAsync(new CourseDaysRequest { Days = [DayOfWeek.Tuesday] });

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<DomainValidationException>(() => scope.WorkPlans.SaveMyPlanAsync(ValidPlan()));

        var request = ValidPlan();
        request.Days.ForEach(d => d.Note = "Boş günüm yok");
        using (var scope = db.NewScope())
        {
            var plan = await scope.WorkPlans.SaveMyPlanAsync(request);
            Assert.All(plan.Days, d => Assert.True(d.IsLessonDay));
        }
    }

    [Fact]
    public async Task SaveMyPlan_HolidayIsRejected()
    {
        var (db, seed) = StudentSession();
        using var _ = db;
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Calendar.AddHolidayAsync(new HolidayRequest { Date = new DateOnly(2026, 11, 10), Name = "Deneme tatili" });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<DomainValidationException>(() => scope.WorkPlans.SaveMyPlanAsync(ValidPlan()));
    }

    [Fact]
    public async Task SaveMyPlan_BeforeWindowOpens_IsRejected()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        var request = new SaveWorkPlanRequest
        {
            Year = 2026,
            Month = 12,
            Days = new[] { 3, 10, 17 }.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 12, d) }).ToList()
        };
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ConflictException>(() => scope.WorkPlans.SaveMyPlanAsync(request));
    }

    [Fact]
    public async Task SaveMyPlan_AfterDeadline_IsRejected()
    {
        var (db, _) = StudentSession();
        using var _ = db;
        db.Clock.AdvanceDays(17);   // 7 Kasım
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ConflictException>(() => scope.WorkPlans.SaveMyPlanAsync(ValidPlan()));
    }

    [Fact]
    public async Task SaveMyPlan_ForPassiveStudent_IsRejected()
    {
        var (db, seed) = StudentSession();
        using var _ = db;
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Students.EndParticipationAsync(seed.MehmetId, new EndParticipationRequest { Reason = "Ayrıldı" });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.WorkPlans.SaveMyPlanAsync(ValidPlan()));
    }

    [Fact]
    public async Task Review_Approve_LocksThePlanForTheStudent_ButNotForAdmin()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        WorkPlanDto plan;
        using (var scope = db.NewScope())
            plan = await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var reviewed = await scope.WorkPlans.ReviewAsync(plan.Id, new ReviewWorkPlanRequest { Approve = true });
            Assert.Equal(WorkPlanStatus.Approved, reviewed.Status);
            Assert.Equal("Ayşe Kaya", reviewed.ReviewedByName);
            Assert.NotNull(reviewed.ReviewedAt);
        }

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.WorkPlans.SaveMyPlanAsync(ValidPlan()));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var changed = await scope.WorkPlans.SaveForStudentAsync(seed.MehmetId, NovemberPlan(4, 10, 17, 24));
            Assert.Equal(WorkPlanStatus.Pending, changed.Status);
            Assert.Null(changed.ReviewedByName);
        }
    }

    [Fact]
    public async Task Review_Reject_RequiresReason_AndKeepsPlanEditableAfterDeadline()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        WorkPlanDto plan;
        using (var scope = db.NewScope())
            plan = await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.WorkPlans.ReviewAsync(plan.Id, new ReviewWorkPlanRequest { Approve = false }));

            var rejected = await scope.WorkPlans.ReviewAsync(plan.Id,
                new ReviewWorkPlanRequest { Approve = false, Note = "Üçüncü hafta değişmeli" });
            Assert.Equal(WorkPlanStatus.Rejected, rejected.Status);
            Assert.Equal("Üçüncü hafta değişmeli", rejected.ReviewNote);
        }

        db.Clock.AdvanceDays(17);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
        {
            var fixedPlan = await scope.WorkPlans.SaveMyPlanAsync(NovemberPlan(4, 10, 17, 24));
            Assert.Equal(WorkPlanStatus.Pending, fixedPlan.Status);
        }
    }

    [Fact]
    public async Task Review_IsOnlyForFacultyAdmin()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        WorkPlanDto plan;
        using (var scope = db.NewScope())
            plan = await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                scope.WorkPlans.ReviewAsync(plan.Id, new ReviewWorkPlanRequest { Approve = true }));
    }

    [Fact]
    public async Task Calendar_ShowsSelectedAndLockedState()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        using (var scope = db.NewScope())
        {
            var calendar = await scope.WorkPlans.GetMyCalendarAsync(2026, 11);
            Assert.True(calendar.CanEdit);
            Assert.Equal(new DateOnly(2026, 11, 5), calendar.EditDeadline);
            Assert.Equal(WorkPlanStatus.Pending, calendar.PlanStatus);
            Assert.Equal(30, calendar.Days.Count);
            Assert.Equal(CalendarColor.Green, calendar.Days.Single(d => d.Date == new DateOnly(2026, 11, 3)).Color);
            Assert.Equal(CalendarColor.Red, calendar.Days.Single(d => d.Date == new DateOnly(2026, 11, 1)).Color);
        }
    }

    [Fact]
    public async Task History_KeepsPreviousMonthsAndIsolatesStudents()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        db.Clock.AdvanceDays(40);   // 30 Kasım
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.WorkPlans.GetMyPlanAsync(2026, 12));

        using (var scope = db.NewScope())
        {
            var history = await scope.WorkPlans.ListMyPlansAsync();
            Assert.Single(history);
        }

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
            Assert.Empty(await scope.WorkPlans.ListMyPlansAsync());
    }

    [Fact]
    public async Task Visibility_FollowsRoleScope()
    {
        var (db, seed) = StudentSession();
        using var _ = db;

        WorkPlanDto plan;
        using (var scope = db.NewScope())
            plan = await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());
        var query = new WorkPlanSearchQuery { Year = 2026, Month = 11 };

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.WorkPlans.SearchAsync(query));

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.WorkPlans.SearchAsync(query));

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
        {
            Assert.Single(await scope.WorkPlans.SearchAsync(query));
            await scope.WorkPlans.GetByIdAsync(plan.Id);
        }

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
        {
            Assert.Empty(await scope.WorkPlans.SearchAsync(query));
            await Assert.ThrowsAsync<NotFoundException>(() => scope.WorkPlans.GetByIdAsync(plan.Id));
        }

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<NotFoundException>(() => scope.WorkPlans.GetByIdAsync(plan.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.WorkPlans.SearchAsync(query));
        }
    }

    [Fact]
    public async Task SaveForStudent_IsOnlyForFacultyAdmin()
    {
        var (db, seed) = StudentSession();
        using var _ = db;
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.WorkPlans.SaveForStudentAsync(seed.MehmetId, ValidPlan()));
    }

    [Fact]
    public async Task Saving_WritesAuditRecordWithoutPersonalData()
    {
        var (db, _) = StudentSession();
        using var _ = db;

        using (var scope = db.NewScope())
            await scope.WorkPlans.SaveMyPlanAsync(ValidPlan());

        using var verify = db.NewContext();
        var log = await verify.AuditLogs.SingleAsync(a => a.Action == AuditActions.WorkPlanSaved);
        Assert.Equal("2026-11, 4 gün", log.Details);
    }
}

public class CourseDayAndCalendarServiceTests
{
    [Fact]
    public async Task CourseDays_AreStoredWithoutDuplicates_AndWeekendIsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using (var scope = db.NewScope())
        {
            var saved = await scope.CourseDays.SetMineAsync(new CourseDaysRequest
            {
                Days = [DayOfWeek.Wednesday, DayOfWeek.Monday, DayOfWeek.Monday]
            });
            Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }, saved);
            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.CourseDays.SetMineAsync(new CourseDaysRequest { Days = [DayOfWeek.Saturday] }));
        }

        using (var scope = db.NewScope())
        {
            await scope.CourseDays.SetMineAsync(new CourseDaysRequest { Days = [DayOfWeek.Wednesday, DayOfWeek.Friday] });
            Assert.Equal(new[] { DayOfWeek.Wednesday, DayOfWeek.Friday }, await scope.CourseDays.GetMineAsync());
        }
    }

    [Fact]
    public async Task Holidays_AreAdminOnly_UniquePerDate_AndVisibleToEveryone()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var request = new HolidayRequest { Date = new DateOnly(2026, 10, 29), Name = "Cumhuriyet Bayramı" };

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Calendar.AddHolidayAsync(request));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await scope.Calendar.AddHolidayAsync(request);
            await Assert.ThrowsAsync<ConflictException>(() => scope.Calendar.AddHolidayAsync(request));
        }

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.Calendar.ListHolidaysAsync(2026));
    }

    [Fact]
    public async Task ExamPeriod_EndBeforeStart_IsRejected_AndDeleteWorks()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Calendar.AddExamPeriodAsync(new ExamPeriodRequest
        {
            Name = "Vize", StartDate = new DateOnly(2026, 11, 20), EndDate = new DateOnly(2026, 11, 10)
        }));

        var period = await scope.Calendar.AddExamPeriodAsync(new ExamPeriodRequest
        {
            Name = "Vize", StartDate = new DateOnly(2026, 11, 9), EndDate = new DateOnly(2026, 11, 13)
        });
        Assert.Single(await scope.Calendar.ListExamPeriodsAsync(2026));

        await scope.Calendar.DeleteExamPeriodAsync(period.Id);
        Assert.Empty(await scope.Calendar.ListExamPeriodsAsync(2026));
    }

    [Fact]
    public async Task ExamPeriod_ExemptsWeekFromMinimumRule()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Calendar.AddExamPeriodAsync(new ExamPeriodRequest
            {
                Name = "Vize", StartDate = new DateOnly(2026, 11, 9), EndDate = new DateOnly(2026, 11, 13)
            });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        var request = new SaveWorkPlanRequest
        {
            Year = 2026, Month = 11,
            Days = new[] { 3, 17, 24 }.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 11, d) }).ToList()
        };
        using (var scope = db.NewScope())
        {
            var plan = await scope.WorkPlans.SaveMyPlanAsync(request);
            Assert.Equal(3, plan.PlannedDays);
        }
    }
}
