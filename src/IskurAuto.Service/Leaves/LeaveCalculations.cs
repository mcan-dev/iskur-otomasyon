using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;

namespace IskurAuto.Service.Leaves;

public enum LeaveWarning
{
    LeaveNearLimit = 1,
    LeavePendingExceeds = 2,
    LeaveLimitExceeded = 3,
    AbsenceNearLimit = 4,
    AbsenceLimitExceeded = 5
}

public sealed record LeaveBalance(
    int Limit,
    decimal Used,
    decimal Pending,
    decimal Remaining,
    decimal HealthLeaveDays,
    decimal UnexcusedAbsenceDays,
    decimal HealthAbsenceDays,
    decimal ExcusedAbsenceDays,
    int AbsenceLimit,
    decimal AbsenceRemaining,
    IReadOnlyList<LeaveWarning> Warnings);

public static class LeaveDayCounter
{
    /// <summary>
    /// İzin hakkından düşülecek gün sayısı. Ay için çalışma planı varsa yalnızca planlı günler sayılır
    /// ; plan yoksa hafta sonu ve tatil olmayan her gün 1 sayılır.
    /// </summary>
    public static decimal Count(DateOnly start, DateOnly end, decimal? hours, IReadOnlySet<DateOnly> holidays,
        IReadOnlyList<MonthlyWorkPlan> plans, WorkPlanSettings settings)
    {
        if (hours is not null)
            return Math.Round(hours.Value / settings.DailyHours, 2);

        var total = 0m;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(day))
                continue;

            var plan = plans.FirstOrDefault(p => p.Year == day.Year && p.Month == day.Month);
            if (plan is null)
            {
                total += 1;
                continue;
            }

            var planned = plan.Days.FirstOrDefault(d => d.Date == day);
            if (planned is not null)
                total += 1;
        }

        return total;
    }
}

public static class LeaveBalanceCalculator
{
    public static LeaveBalance Calculate(LeavePolicy policy, IEnumerable<LeaveRequest> leaves, IEnumerable<Absence> absences)
    {
        var all = leaves.ToList();
        var counted = all
            .Where(l => l.Type != LeaveType.Health || policy.HealthLeaveCountsTowardLimit)
            .ToList();

        var used = counted.Where(l => l.Status == LeaveStatus.Approved).Sum(l => l.DaysCounted);
        var pending = counted.Where(l => l.Status == LeaveStatus.Pending).Sum(l => l.DaysCounted);
        var healthLeave = all.Where(l => l.Type == LeaveType.Health && l.Status == LeaveStatus.Approved).Sum(l => l.DaysCounted);

        var active = absences.Where(a => !a.IsRevoked).ToList();
        var unexcused = active.Count(a => a.Type == AbsenceType.Unexcused);

        var remaining = policy.LeaveDayLimit - used;
        var absenceRemaining = policy.AbsenceDayLimit - unexcused;

        return new LeaveBalance(
            policy.LeaveDayLimit, used, pending, remaining, healthLeave,
            unexcused,
            active.Count(a => a.Type == AbsenceType.HealthReport),
            active.Count(a => a.Type == AbsenceType.Excused),
            policy.AbsenceDayLimit, absenceRemaining,
            Warnings(policy, used, pending, remaining, unexcused, absenceRemaining));
    }

    private static List<LeaveWarning> Warnings(LeavePolicy policy, decimal used, decimal pending, decimal remaining,
        int unexcused, decimal absenceRemaining)
    {
        var warnings = new List<LeaveWarning>();

        if (used > policy.LeaveDayLimit)
            warnings.Add(LeaveWarning.LeaveLimitExceeded);
        else if (used + pending > policy.LeaveDayLimit)
            warnings.Add(LeaveWarning.LeavePendingExceeds);
        else if (used + pending > 0 && remaining <= policy.LeaveWarningRemainingDays)
            warnings.Add(LeaveWarning.LeaveNearLimit);

        if (unexcused > policy.AbsenceDayLimit)
            warnings.Add(LeaveWarning.AbsenceLimitExceeded);
        else if (unexcused > 0 && absenceRemaining <= policy.AbsenceWarningRemainingDays)
            warnings.Add(LeaveWarning.AbsenceNearLimit);

        return warnings;
    }
}
