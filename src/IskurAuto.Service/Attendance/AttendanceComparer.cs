using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.Attendance;

public enum PlannedDayOutcome
{
    /// <summary>O gün karekodla (veya elle) devam kaydı var.</summary>
    Attended = 1,

    /// <summary>Onaylı günlük izin var.</summary>
    OnLeave = 2,

    /// <summary>Devamsızlık kaydı girilmiş.</summary>
    RecordedAbsence = 3,

    /// <summary>Gün geçti; devam, izin ya da devamsızlık kaydı yok. İdari inceleme gerekir.</summary>
    Missing = 4,

    /// <summary>Bugün; öğrenci hâlâ okutabilir.</summary>
    Pending = 5,

    /// <summary>Henüz gelmemiş planlı gün.</summary>
    Upcoming = 6
}

public sealed record PlannedDayResult(DateOnly Date, WorkShift Shift, PlannedDayOutcome Outcome);

public sealed record AttendanceComparison(IReadOnlyList<PlannedDayResult> Days)
{
    public decimal PlannedDays => Days.Sum(d => Weight(d));
    public decimal AttendedDays => Sum(PlannedDayOutcome.Attended);
    public decimal LeaveDays => Sum(PlannedDayOutcome.OnLeave);
    public decimal RecordedAbsenceDays => Sum(PlannedDayOutcome.RecordedAbsence);
    public decimal MissingDays => Sum(PlannedDayOutcome.Missing);

    private decimal Sum(PlannedDayOutcome outcome) => Days.Where(d => d.Outcome == outcome).Sum(Weight);

    private static decimal Weight(PlannedDayResult day) => day.Shift == WorkShift.FullDay ? 1m : 0.5m;
}

/// <summary>
/// Planlanan çalışma günleri ile gerçekleşen devamı karşılaştırır (belge madde 6, son paragraf).
/// Saf bir sınıftır: veriyi servis toplar, kararı burası verir.
/// </summary>
public static class AttendanceComparer
{
    public static AttendanceComparison Compare(
        IEnumerable<WorkPlanDay> plannedDays,
        IReadOnlySet<DateOnly> attendedDates,
        IEnumerable<LeaveRequest> approvedLeaves,
        IReadOnlySet<DateOnly> absenceDates,
        DateOnly today)
    {
        var fullDayLeaves = approvedLeaves.Where(l => l.Hours is null).ToList();

        var results = plannedDays
            .OrderBy(d => d.Date)
            .Select(d => new PlannedDayResult(d.Date, d.Shift,
                Outcome(d.Date, attendedDates, fullDayLeaves, absenceDates, today)))
            .ToList();

        return new AttendanceComparison(results);
    }

    private static PlannedDayOutcome Outcome(DateOnly date, IReadOnlySet<DateOnly> attended,
        List<LeaveRequest> leaves, IReadOnlySet<DateOnly> absences, DateOnly today)
    {
        if (attended.Contains(date)) return PlannedDayOutcome.Attended;
        if (leaves.Any(l => l.StartDate <= date && date <= l.EndDate)) return PlannedDayOutcome.OnLeave;
        if (absences.Contains(date)) return PlannedDayOutcome.RecordedAbsence;
        if (date > today) return PlannedDayOutcome.Upcoming;
        return date == today ? PlannedDayOutcome.Pending : PlannedDayOutcome.Missing;
    }
}
