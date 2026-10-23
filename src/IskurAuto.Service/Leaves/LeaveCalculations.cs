using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;

namespace IskurAuto.Service.Leaves;

public enum LeaveWarning
{
    LeaveNearLimit = 1,
    LeavePendingExceeds = 2,
    LeaveLimitExceeded = 3
}

/// <summary>
/// İzin bakiyesi. Tek bir hak vardır: onaylı izin günleri ve devamsızlık günleri aynı haktan düşer.
/// </summary>
public sealed record LeaveBalance(
    int Limit,
    decimal Used,
    decimal LeaveDays,
    decimal AbsenceDays,
    decimal Pending,
    decimal Remaining,
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
    /// <param name="limitOverride">Öğrenciye özel hak; boşsa genel hak kullanılır.</param>
    public static LeaveBalance Calculate(LeavePolicy policy, IEnumerable<LeaveRequest> leaves, IEnumerable<Absence> absences,
        int? limitOverride = null)
    {
        var limit = limitOverride ?? policy.LeaveDayLimit;
        var all = leaves.ToList();

        var leaveDays = all.Where(l => l.Status == LeaveStatus.Approved).Sum(l => l.DaysCounted);
        var pending = all.Where(l => l.Status == LeaveStatus.Pending).Sum(l => l.DaysCounted);
        var absenceDays = absences.Count(a => !a.IsRevoked);

        var used = leaveDays + absenceDays;
        var remaining = limit - used;

        return new LeaveBalance(limit, used, leaveDays, absenceDays, pending, remaining,
            Warnings(policy, limit, used, pending, remaining));
    }

    private static List<LeaveWarning> Warnings(LeavePolicy policy, int limit, decimal used, decimal pending, decimal remaining)
    {
        var warnings = new List<LeaveWarning>();

        if (used > limit)
            warnings.Add(LeaveWarning.LeaveLimitExceeded);
        else if (used + pending > limit)
            warnings.Add(LeaveWarning.LeavePendingExceeds);
        else if (used + pending > 0 && remaining <= policy.LeaveWarningRemainingDays)
            warnings.Add(LeaveWarning.LeaveNearLimit);

        return warnings;
    }
}
