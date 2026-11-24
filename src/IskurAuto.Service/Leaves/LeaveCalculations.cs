using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;

namespace IskurAuto.Service.Leaves;

public enum LeaveWarning
{
    LeaveNearLimit = 1,
    LeaveLimitExceeded = 3
}

/// <summary>
/// İzin bakiyesi. Tek bir hak vardır: izin günleri ve devamsızlık günleri aynı haktan düşer.
/// </summary>
public sealed record LeaveBalance(
    int Limit,
    decimal Used,
    decimal LeaveDays,
    decimal AbsenceDays,
    decimal Remaining,
    IReadOnlyList<LeaveWarning> Warnings);

public static class LeaveBalanceCalculator
{
    /// <param name="limitOverride">Öğrenciye özel hak; boşsa genel hak kullanılır.</param>
    public static LeaveBalance Calculate(LeavePolicy policy, IEnumerable<LeaveRequest> leaves, IEnumerable<Absence> absences,
        int? limitOverride = null)
    {
        var limit = limitOverride ?? policy.LeaveDayLimit;

        var leaveDays = leaves.Where(l => l.Status == LeaveStatus.Approved).Sum(l => l.DaysCounted);
        var absenceDays = absences.Count(a => !a.IsRevoked);

        var used = leaveDays + absenceDays;
        // Eksi izin yoktur: hak bittiyse kalan 0'dır ve yeni izin alınamaz.
        var remaining = Math.Max(0m, limit - used);

        return new LeaveBalance(limit, used, leaveDays, absenceDays, remaining, Warnings(policy, limit, used, remaining));
    }

    private static List<LeaveWarning> Warnings(LeavePolicy policy, int limit, decimal used, decimal remaining)
    {
        var warnings = new List<LeaveWarning>();

        if (used > limit)
            warnings.Add(LeaveWarning.LeaveLimitExceeded);
        else if (used > 0 && remaining <= policy.LeaveWarningRemainingDays)
            warnings.Add(LeaveWarning.LeaveNearLimit);

        return warnings;
    }
}
