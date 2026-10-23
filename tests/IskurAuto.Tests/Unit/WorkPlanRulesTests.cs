using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Mapping;
using IskurAuto.Service.WorkPlans;
using Xunit;

namespace IskurAuto.Tests.Unit;

public class WorkPlanRulesTests
{
    // Kasım 2026: 1 Kasım pazar. Perşembesi Kasım'a düşen haftalar 2, 9, 16 ve 23 Kasım haftalarıdır.
    private static readonly WorkPlanSettings Settings = new();

    private static PlanMonth November(
        IEnumerable<DateOnly>? holidays = null,
        IEnumerable<ExamPeriod>? exams = null,
        IEnumerable<DayOfWeek>? lessonDays = null,
        ProgramPeriod? period = null)
        => new(2026, 11, holidays ?? [], exams ?? [], (lessonDays ?? []).Select(Lesson),
            [period ?? new ProgramPeriod(new DateOnly(2026, 9, 1), null)]);

    private static StudentCourseDay Lesson(DayOfWeek day) => new() { DayOfWeek = day };

    private static PlannedDay Day(int day, string? note = null)
        => new(new DateOnly(2026, 11, day), note);

    private static PlannedDay[] OnePerWeek() => [Day(3), Day(10), Day(17), Day(24)];

    [Fact]
    public void Validate_OneDayPerWeek_HasNoErrors()
        => Assert.Empty(WorkPlanRules.Validate(November(), OnePerWeek(), Settings));

    [Fact]
    public void Validate_FourDaysInOneWeek_ReportsMaximum()
    {
        var days = new[] { Day(2), Day(3), Day(4), Day(5), Day(10), Day(17), Day(24) };

        var errors = WorkPlanRules.Validate(November(), days, Settings);

        var error = Assert.Single(errors);
        Assert.Contains("en fazla 3", error);
    }

    [Fact]
    public void Validate_ThreeDaysInOneWeek_IsAllowed()
    {
        var days = new[] { Day(2), Day(3), Day(4), Day(10), Day(17), Day(24) };

        Assert.Empty(WorkPlanRules.Validate(November(), days, Settings));
    }

    [Fact]
    public void Validate_WeekWithoutAnyDay_ReportsMinimum()
    {
        var days = new[] { Day(3), Day(17), Day(24) };

        var error = Assert.Single(WorkPlanRules.Validate(November(), days, Settings));

        Assert.Contains("09.11.2026", error);
        Assert.Contains("en az 1", error);
    }

    [Fact]
    public void Validate_WeekBelongingToNextMonth_IsNotRequired()
    {
        // 30 Kasım haftasının perşembesi Aralık'ta; Kasım planında bu hafta için gün istenmez.
        Assert.Empty(WorkPlanRules.Validate(November(), OnePerWeek(), Settings));
    }

    [Fact]
    public void Validate_ExamWeek_IsExemptFromMinimum()
    {
        var exam = new ExamPeriod { Name = "Vize", StartDate = new DateOnly(2026, 11, 9), EndDate = new DateOnly(2026, 11, 13) };
        var days = new[] { Day(3), Day(17), Day(24) };

        Assert.Empty(WorkPlanRules.Validate(November(exams: [exam]), days, Settings));
    }

    [Fact]
    public void Validate_WeekWhereEveryWeekdayIsHoliday_IsNotRequired()
    {
        var holidays = Enumerable.Range(9, 5).Select(d => new DateOnly(2026, 11, d));
        var days = new[] { Day(3), Day(17), Day(24) };

        Assert.Empty(WorkPlanRules.Validate(November(holidays: holidays), days, Settings));
    }

    [Fact]
    public void Validate_Weekend_IsRejected()
    {
        var days = OnePerWeek().Append(Day(7)).ToArray();

        var error = Assert.Single(WorkPlanRules.Validate(November(), days, Settings));

        Assert.Contains("hafta sonu", error);
    }

    [Fact]
    public void Validate_Holiday_IsRejected()
    {
        var days = new[] { Day(3), Day(10), Day(17), Day(24) };
        var holidays = new[] { new DateOnly(2026, 11, 10) };

        var errors = WorkPlanRules.Validate(November(holidays: holidays), days, Settings);

        Assert.Contains(errors, e => e.Contains("resmî tatil"));
    }

    [Fact]
    public void Validate_LessonDayWithoutNote_IsRejected()
    {
        var days = new[] { Day(3), Day(10), Day(17), Day(24) };

        var errors = WorkPlanRules.Validate(November(lessonDays: [DayOfWeek.Tuesday]), days, Settings);

        Assert.Equal(4, errors.Count);
        Assert.All(errors, e => Assert.Contains("açıklama", e));
    }

    [Fact]
    public void Validate_LessonDayWithNote_IsAllowed()
    {
        var days = new[] { Day(3, "Boş günüm yok"), Day(10, "Boş günüm yok"), Day(17, "Boş günüm yok"), Day(24, "Boş günüm yok") };

        Assert.Empty(WorkPlanRules.Validate(November(lessonDays: [DayOfWeek.Tuesday]), days, Settings));
    }

    [Fact]
    public void Validate_DayOutsideProgramPeriod_IsRejected()
    {
        var period = new ProgramPeriod(new DateOnly(2026, 11, 16), null);
        var days = new[] { Day(3), Day(17), Day(24) };

        var errors = WorkPlanRules.Validate(November(period: period), days, Settings);

        Assert.Contains(errors, e => e.Contains("programa katılım"));
    }

    [Fact]
    public void Validate_ClosedProgramPeriod_BlocksDaysAfterEnd()
    {
        var period = new ProgramPeriod(new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 13));
        var days = new[] { Day(3), Day(10), Day(17), Day(24) };

        var errors = WorkPlanRules.Validate(November(period: period), days, Settings);

        Assert.Equal(2, errors.Count(e => e.Contains("programa katılım")));
    }

    [Fact]
    public void Validate_DuplicateAndOutOfMonthDates_AreRejected()
    {
        var days = OnePerWeek().Append(Day(3)).Append(new PlannedDay(new DateOnly(2026, 12, 1), null)).ToArray();

        var errors = WorkPlanRules.Validate(November(), days, Settings);

        Assert.Contains(errors, e => e.Contains("birden fazla"));
        Assert.Contains(errors, e => e.Contains("bu ayın içinde değil"));
    }

}

public class WorkPlanSettingsTests
{
    [Fact]
    public void DailyHours_DefaultsToEight()
        => Assert.Equal(8m, new WorkPlanSettings().DailyHours);
}

public class WorkPlanCalendarTests
{
    private static PlanMonth November(IEnumerable<DateOnly>? holidays = null, IEnumerable<DayOfWeek>? lessonDays = null)
        => new(2026, 11, holidays ?? [], [], (lessonDays ?? []).Select(Lesson), [new ProgramPeriod(new DateOnly(2026, 9, 1), null)]);

    private static StudentCourseDay Lesson(DayOfWeek day) => new() { DayOfWeek = day };

    private static CalendarDayDto Find(List<CalendarDayDto> calendar, int day)
        => calendar.Single(d => d.Date == new DateOnly(2026, 11, day));

    [Fact]
    public void ToCalendar_ReturnsEveryDayOfTheMonth()
        => Assert.Equal(30, November().ToCalendar(null).Count);

    [Fact]
    public void ToCalendar_WeekendAndHolidayAreRed()
    {
        var calendar = November([new DateOnly(2026, 11, 11)]).ToCalendar(null);

        Assert.Equal(CalendarColor.Red, Find(calendar, 7).Color);
        Assert.Equal(CalendarColor.Red, Find(calendar, 11).Color);
        Assert.False(Find(calendar, 11).IsSelectable);
    }

    [Fact]
    public void ToCalendar_FreeDayIsGray_SelectedDayIsGreen()
    {
        var plan = new MonthlyWorkPlan();
        plan.Days.Add(new WorkPlanDay { Date = new DateOnly(2026, 11, 3) });

        var calendar = November().ToCalendar(plan);

        Assert.Equal(CalendarColor.Gray, Find(calendar, 4).Color);
        var selected = Find(calendar, 3);
        Assert.Equal(CalendarColor.Green, selected.Color);
        Assert.True(selected.IsSelected);
        Assert.Equal("Seçildi", selected.Label);
    }

    [Fact]
    public void ToCalendar_LessonDayIsYellow_EvenWhenSelected()
    {
        var plan = new MonthlyWorkPlan();
        plan.Days.Add(new WorkPlanDay { Date = new DateOnly(2026, 11, 3), IsLessonDay = true });

        var calendar = November(lessonDays: [DayOfWeek.Tuesday]).ToCalendar(plan);

        Assert.Equal(CalendarColor.Yellow, Find(calendar, 3).Color);
        Assert.Equal(CalendarColor.Yellow, Find(calendar, 10).Color);
    }
}
