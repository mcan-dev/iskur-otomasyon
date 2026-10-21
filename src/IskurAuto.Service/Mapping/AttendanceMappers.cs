using IskurAuto.Core.Entities;
using IskurAuto.Service.Attendance;
using IskurAuto.Service.DTOs;

namespace IskurAuto.Service.Mapping;

public static class AttendanceMappers
{
    public static SessionDto ToDto(this AttendanceSession s, DateTime localNow) => new()
    {
        Id = s.Id,
        Date = s.Date,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Title = s.Title,
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department?.Name,
        CreatedByName = s.CreatedByUser.FullName,
        IsClosed = s.IsClosed,
        IsActiveNow = s.IsActiveAt(localNow)
    };

    public static bool IsActiveAt(this AttendanceSession s, DateTime localNow)
    {
        var time = TimeOnly.FromDateTime(localNow);
        return !s.IsClosed && s.Date == DateOnly.FromDateTime(localNow) && s.StartTime <= time && time <= s.EndTime;
    }

    public static AttendanceDto ToDto(this AttendanceRecord r) => new()
    {
        Id = r.Id,
        StudentId = r.StudentProfileId,
        StudentName = r.StudentProfile.User.FullName,
        DepartmentName = r.StudentProfile.Department.Name,
        SessionId = r.AttendanceSessionId,
        Date = r.Date,
        SessionStart = r.AttendanceSession.StartTime,
        SessionEnd = r.AttendanceSession.EndTime,
        CheckedInAt = r.CheckedInAt,
        IsManual = r.IsManual,
        ManualReason = r.ManualReason,
        CreatedByName = r.CreatedByUser?.FullName,
        IsVoided = r.IsVoided,
        VoidedAt = r.VoidedAt,
        VoidedByName = r.VoidedByUser?.FullName,
        VoidReason = r.VoidReason
    };

    public static AttendanceComparisonDto ToDto(this AttendanceComparison c, int studentId, int year, int month, bool hasPlan) => new()
    {
        StudentId = studentId,
        Year = year,
        Month = month,
        HasApprovedPlan = hasPlan,
        PlannedDays = c.PlannedDays,
        AttendedDays = c.AttendedDays,
        LeaveDays = c.LeaveDays,
        RecordedAbsenceDays = c.RecordedAbsenceDays,
        MissingDays = c.MissingDays,
        Days = c.Days.Select(d => new PlannedDayDto { Date = d.Date, Shift = d.Shift, Outcome = d.Outcome }).ToList()
    };
}
