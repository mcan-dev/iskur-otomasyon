using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Service.Attendance;
using Xunit;

namespace IskurAuto.Tests.Unit;

public class AttendanceComparerTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    private static WorkPlanDay Day(int day)
        => new() { Date = new DateOnly(2026, 10, day) };

    private static LeaveRequest Leave(int from, int to, decimal? hours = null)
        => new() { StartDate = new DateOnly(2026, 10, from), EndDate = new DateOnly(2026, 10, to), Hours = hours, Status = LeaveStatus.Approved };

    private static HashSet<DateOnly> Dates(params int[] days)
        => days.Select(d => new DateOnly(2026, 10, d)).ToHashSet();

    private static PlannedDayOutcome OutcomeOf(AttendanceComparison c, int day)
        => c.Days.Single(d => d.Date.Day == day).Outcome;

    [Fact]
    public void EachPlannedDay_GetsTheExpectedOutcome()
    {
        var result = AttendanceComparer.Compare(
            new[] { Day(5), Day(6), Day(7), Day(8), Day(21), Day(28) },
            attendedDates: Dates(5),
            approvedLeaves: new[] { Leave(6, 6) },
            absenceDates: Dates(7),
            today: Today);

        Assert.Equal(PlannedDayOutcome.Attended, OutcomeOf(result, 5));
        Assert.Equal(PlannedDayOutcome.OnLeave, OutcomeOf(result, 6));
        Assert.Equal(PlannedDayOutcome.RecordedAbsence, OutcomeOf(result, 7));
        Assert.Equal(PlannedDayOutcome.Missing, OutcomeOf(result, 8));
        Assert.Equal(PlannedDayOutcome.Pending, OutcomeOf(result, 21));
        Assert.Equal(PlannedDayOutcome.Upcoming, OutcomeOf(result, 28));
    }

    [Fact]
    public void Totals_CountEveryPlannedDayAsOne()
    {
        var result = AttendanceComparer.Compare(
            new[] { Day(5), Day(6), Day(7) },
            Dates(5, 6), Array.Empty<LeaveRequest>(), Dates(), Today);

        Assert.Equal(3m, result.PlannedDays);
        Assert.Equal(2m, result.AttendedDays);
        Assert.Equal(1m, result.MissingDays);
    }

    [Fact]
    public void Attendance_WinsOverLeave_WhenBothExist()
    {
        var result = AttendanceComparer.Compare(new[] { Day(5) }, Dates(5), new[] { Leave(5, 5) }, Dates(), Today);
        Assert.Equal(PlannedDayOutcome.Attended, OutcomeOf(result, 5));
    }

    [Fact]
    public void HourlyLeave_DoesNotExcuseTheWholeDay()
    {
        var result = AttendanceComparer.Compare(new[] { Day(5) }, Dates(), new[] { Leave(5, 5, hours: 2) }, Dates(), Today);
        Assert.Equal(PlannedDayOutcome.Missing, OutcomeOf(result, 5));
    }

    [Fact]
    public void LeaveRange_CoversEveryDayInside()
    {
        var result = AttendanceComparer.Compare(new[] { Day(5), Day(6), Day(7) }, Dates(), new[] { Leave(5, 7) }, Dates(), Today);
        Assert.Equal(3m, result.LeaveDays);
    }

    [Fact]
    public void NoPlan_GivesAnEmptyComparison()
    {
        var result = AttendanceComparer.Compare(Array.Empty<WorkPlanDay>(), Dates(5), Array.Empty<LeaveRequest>(), Dates(), Today);
        Assert.Empty(result.Days);
        Assert.Equal(0m, result.PlannedDays);
    }
}
