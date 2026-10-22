using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;
using IskurAuto.Service.Leaves;
using Xunit;

namespace IskurAuto.Tests.Unit;

public class LeaveDayCounterTests
{
    private static readonly WorkPlanSettings Settings = new();
    private static readonly IReadOnlySet<DateOnly> NoHolidays = new HashSet<DateOnly>();

    private static DateOnly Nov(int day) => new(2026, 11, day);

    private static MonthlyWorkPlan Plan(params int[] days)
    {
        var plan = new MonthlyWorkPlan { Year = 2026, Month = 11 };
        foreach (var day in days)
            plan.Days.Add(new WorkPlanDay { Date = Nov(day) });
        return plan;
    }

    [Fact]
    public void Count_WithoutPlan_CountsEveryWeekday()
        => Assert.Equal(5m, LeaveDayCounter.Count(Nov(2), Nov(6), null, NoHolidays, [], Settings));

    [Fact]
    public void Count_SkipsWeekendsAndHolidays()
    {
        var holidays = new HashSet<DateOnly> { Nov(4) };

        Assert.Equal(4m, LeaveDayCounter.Count(Nov(2), Nov(8), null, holidays, [], Settings));
    }

    [Fact]
    public void Count_WithPlan_CountsOnlyPlannedDays()
    {
        var plans = new[] { Plan(3, 5) };

        Assert.Equal(2m, LeaveDayCounter.Count(Nov(2), Nov(6), null, NoHolidays, plans, Settings));
    }

    [Fact]
    public void Count_WithPlanButNoPlannedDayInRange_IsZero()
    {
        var plans = new[] { Plan(10) };

        Assert.Equal(0m, LeaveDayCounter.Count(Nov(2), Nov(6), null, NoHolidays, plans, Settings));
    }

    [Fact]
    public void Count_MonthWithoutPlan_StillCountsWeekdays()
    {
        var plans = new[] { Plan(3) };

        Assert.Equal(2m, LeaveDayCounter.Count(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2), null, NoHolidays, plans, Settings));
    }

    [Fact]
    public void Count_HourlyLeave_IsFractionOfFullDay()
        => Assert.Equal(0.5m, LeaveDayCounter.Count(Nov(3), Nov(3), 4m, NoHolidays, [], Settings));
}

public class LeaveBalanceCalculatorTests
{
    private static readonly LeavePolicy Policy = new();

    private static LeaveRequest Leave(decimal days, LeaveStatus status = LeaveStatus.Approved, LeaveType type = LeaveType.Excuse)
        => new() { DaysCounted = days, Status = status, Type = type };

    private static Absence Absent(AbsenceType type = AbsenceType.Unexcused, bool revoked = false)
        => new() { Type = type, RevokedAt = revoked ? DateTime.UtcNow : null };

    private static void AssertWarnings(LeaveBalance balance, params LeaveWarning[] expected)
        => Assert.True(expected.SequenceEqual(balance.Warnings),
            $"Uyarılar: {string.Join(", ", balance.Warnings)}");

    private static LeaveBalance Calc(IEnumerable<LeaveRequest>? leaves = null, IEnumerable<Absence>? absences = null, LeavePolicy? policy = null)
        => LeaveBalanceCalculator.Calculate(policy ?? Policy, leaves ?? [], absences ?? []);

    [Fact]
    public void NoRecords_FullBalanceAndNoWarnings()
    {
        var balance = Calc();

        Assert.Equal(10m, balance.Remaining);
        Assert.Empty(balance.Warnings);
    }

    [Fact]
    public void ApprovedLeaves_ReduceRemaining_PendingDoesNot()
    {
        var balance = Calc([Leave(3), Leave(2, LeaveStatus.Pending)]);

        Assert.Equal(3m, balance.Used);
        Assert.Equal(2m, balance.Pending);
        Assert.Equal(7m, balance.Remaining);
        Assert.Empty(balance.Warnings);
    }

    [Fact]
    public void RejectedAndCancelledLeaves_AreIgnored()
    {
        var balance = Calc([Leave(4, LeaveStatus.Rejected), Leave(4, LeaveStatus.Cancelled)]);

        Assert.Equal(0m, balance.Used + balance.Pending);
    }

    [Fact]
    public void NearLimit_WarnsWhenRemainingIsAtOrBelowThreshold()
    {
        var balance = Calc([Leave(8)]);

        AssertWarnings(balance, LeaveWarning.LeaveNearLimit);
    }

    [Fact]
    public void PendingRequestsThatWouldExceed_AreFlagged()
    {
        var balance = Calc([Leave(5), Leave(6, LeaveStatus.Pending)]);

        AssertWarnings(balance, LeaveWarning.LeavePendingExceeds);
    }

    [Fact]
    public void ExactlyAtLimit_IsNotExceeded()
    {
        var balance = Calc([Leave(10)]);

        Assert.Equal(0m, balance.Remaining);
        AssertWarnings(balance, LeaveWarning.LeaveNearLimit);
    }

    [Fact]
    public void AboveLimit_IsExceeded()
    {
        var balance = Calc([Leave(11)]);

        AssertWarnings(balance, LeaveWarning.LeaveLimitExceeded);
    }

    [Fact]
    public void HealthLeave_IsTrackedSeparately_ByDefault()
    {
        var balance = Calc([Leave(6, type: LeaveType.Health)]);

        Assert.Equal(0m, balance.Used);
        Assert.Equal(6m, balance.HealthLeaveDays);
        Assert.Empty(balance.Warnings);
    }

    [Fact]
    public void HealthLeave_CountsWhenPolicySaysSo()
    {
        var policy = new LeavePolicy { HealthLeaveCountsTowardLimit = true };

        var balance = Calc([Leave(6, type: LeaveType.Health)], policy: policy);

        Assert.Equal(6m, balance.Used);
    }

    [Fact]
    public void LimitComesFromPolicy()
    {
        var policy = new LeavePolicy { LeaveDayLimit = 5 };

        var balance = Calc([Leave(6)], policy: policy);

        Assert.Equal(5, balance.Limit);
        AssertWarnings(balance, LeaveWarning.LeaveLimitExceeded);
    }

    [Fact]
    public void Absences_OnlyUnexcusedCountTowardLimit()
    {
        var balance = Calc(absences: [Absent(), Absent(AbsenceType.HealthReport), Absent(AbsenceType.Excused)]);

        Assert.Equal(1m, balance.UnexcusedAbsenceDays);
        Assert.Equal(1m, balance.HealthAbsenceDays);
        Assert.Equal(1m, balance.ExcusedAbsenceDays);
    }

    [Fact]
    public void Absences_RevokedAreIgnored()
    {
        var balance = Calc(absences: [Absent(revoked: true), Absent(revoked: true)]);

        Assert.Equal(0m, balance.UnexcusedAbsenceDays);
    }

    [Fact]
    public void Absences_WarnNearAndOverLimit()
    {
        var near = Calc(absences: Enumerable.Range(0, 4).Select(_ => Absent()));
        var over = Calc(absences: Enumerable.Range(0, 6).Select(_ => Absent()));

        AssertWarnings(near, LeaveWarning.AbsenceNearLimit);
        AssertWarnings(over, LeaveWarning.AbsenceLimitExceeded);
    }
}
