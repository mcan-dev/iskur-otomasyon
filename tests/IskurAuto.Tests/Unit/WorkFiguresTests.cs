using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Service.Reports;
using Xunit;

namespace IskurAuto.Tests.Unit;

public class WorkFiguresTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    private static MonthlyWorkPlan Plan(WorkPlanStatus status, params (int Day, WorkShift Shift)[] days) => new()
    {
        Status = status,
        Days = days.Select(d => new WorkPlanDay { Date = new DateOnly(2026, 10, d.Day), Shift = d.Shift }).ToList()
    };

    private static HashSet<DateOnly> Dates(params int[] days) => days.Select(d => new DateOnly(2026, 10, d)).ToHashSet();

    [Fact]
    public void Hours_AreAttendedDaysTimesFullDayHours_AndHalfDaysCountHalf()
    {
        var plan = Plan(WorkPlanStatus.Approved, (6, WorkShift.FullDay), (14, WorkShift.Morning), (20, WorkShift.FullDay));

        var figures = WorkFigures.Compute(plan, Dates(6, 14), Array.Empty<LeaveRequest>(), Dates(), Today, fullDayHours: 8);

        Assert.Equal(1.5m, figures.AttendedDays);
        Assert.Equal(12m, figures.Hours);
        Assert.Equal(2.5m, figures.PlannedDays);
        Assert.Equal(1m, figures.MissingDays);
    }

    [Theory]
    [InlineData(WorkPlanStatus.Pending)]
    [InlineData(WorkPlanStatus.Rejected)]
    public void APlanThatIsNotApproved_CountsForNothing(WorkPlanStatus status)
    {
        var figures = WorkFigures.Compute(Plan(status, (6, WorkShift.FullDay)), Dates(6), Array.Empty<LeaveRequest>(), Dates(), Today, 8);
        Assert.Equal(WorkFigures.Empty, figures);
    }

    [Fact]
    public void NoPlan_GivesEmptyFigures()
        => Assert.Equal(WorkFigures.Empty, WorkFigures.Compute(null, Dates(6), Array.Empty<LeaveRequest>(), Dates(), Today, 8));

    [Fact]
    public void AttendanceOnAnUnplannedDay_IsNotCountedAsWork()
    {
        var plan = Plan(WorkPlanStatus.Approved, (6, WorkShift.FullDay));

        var figures = WorkFigures.Compute(plan, Dates(7), Array.Empty<LeaveRequest>(), Dates(), Today, 8);

        Assert.Equal(0m, figures.Hours);
    }
}
