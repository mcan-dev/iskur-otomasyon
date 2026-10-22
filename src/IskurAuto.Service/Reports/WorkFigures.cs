using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Service.Attendance;

namespace IskurAuto.Service.Reports;

/// <summary>
/// Bir öğrencinin bir aydaki planlanan ve gerçekleşen çalışması. Yalnızca onaylı plandaki günler sayılır;
/// plan dışı bir güne yazılan devam kaydı çalışma saatine eklenmez.
/// </summary>
public sealed record WorkFigures(
    decimal PlannedDays,
    decimal AttendedDays,
    decimal LeaveDays,
    decimal RecordedAbsenceDays,
    decimal MissingDays,
    decimal Hours)
{
    public static readonly WorkFigures Empty = new(0, 0, 0, 0, 0, 0);

    public static WorkFigures Compute(
        MonthlyWorkPlan? plan,
        IReadOnlySet<DateOnly> attendedDates,
        IEnumerable<LeaveRequest> approvedLeaves,
        IReadOnlySet<DateOnly> absenceDates,
        DateOnly today,
        decimal dailyHours)
    {
        if (plan is not { Status: WorkPlanStatus.Approved })
            return Empty;

        var comparison = AttendanceComparer.Compare(plan.Days, attendedDates, approvedLeaves, absenceDates, today);

        return new WorkFigures(
            comparison.PlannedDays,
            comparison.AttendedDays,
            comparison.LeaveDays,
            comparison.RecordedAbsenceDays,
            comparison.MissingDays,
            comparison.AttendedDays * dailyHours);
    }
}
