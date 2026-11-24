using IskurAuto.Core.Entities;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Leaves;

namespace IskurAuto.Service.Mapping;

public static class LeaveMappers
{
    public static LeaveDto ToDto(this LeaveRequest l) => new()
    {
        Id = l.Id,
        StudentId = l.StudentProfileId,
        StudentName = l.StudentProfile.User.FullName,
        StudentNumber = l.StudentProfile.StudentNumber,
        DepartmentName = l.StudentProfile.Department.Name,
        Status = l.Status,
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        DaysCounted = l.DaysCounted,
        Description = l.Description,
        AppliedOn = l.AppliedOn,
        CancelledByName = l.CancelledByUser?.FullName,
        CancelledAt = l.CancelledAt,
        CancelNote = l.CancelNote
    };

    public static AbsenceDto ToDto(this Absence a) => new()
    {
        Id = a.Id,
        StudentId = a.StudentProfileId,
        StudentName = a.StudentProfile.User.FullName,
        DepartmentName = a.StudentProfile.Department.Name,
        Date = a.Date,
        Type = a.Type,
        Note = a.Note,
        RecordedByName = a.RecordedByUser.FullName,
        IsRevoked = a.IsRevoked,
        RevokedAt = a.RevokedAt,
        RevokedByName = a.RevokedByUser?.FullName,
        RevokeReason = a.RevokeReason
    };

    public static LeaveBalanceDto ToDto(this LeaveBalance b, int year) => new()
    {
        Year = year,
        Limit = b.Limit,
        Used = b.Used,
        Remaining = b.Remaining,
        LeaveDays = b.LeaveDays,
        AbsenceDays = b.AbsenceDays,
        Warnings = b.Warnings.Select(w => new WarningDto { Code = w.ToString(), Message = Message(w, b) }).ToList()
    };

    private static string Message(LeaveWarning warning, LeaveBalance b) => warning switch
    {
        LeaveWarning.LeaveNearLimit => $"İzin hakkı azalıyor: {b.Remaining:0.##} gün kaldı.",
        _ => $"İzin hakkı aşıldı ({b.Used:0.##}/{b.Limit} gün)."
    };
}
