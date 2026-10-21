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
        DepartmentName = l.StudentProfile.Department.Name,
        Type = l.Type,
        Status = l.Status,
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        Hours = l.Hours,
        DaysCounted = l.DaysCounted,
        Reason = l.Reason,
        Description = l.Description,
        AppliedOn = l.AppliedOn,
        ReviewedByName = l.ReviewedByUser?.FullName,
        ReviewedAt = l.ReviewedAt,
        ReviewNote = l.ReviewNote,
        HasDocument = l.HasDocument,
        DocumentFileName = l.DocumentFileName
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
        Pending = b.Pending,
        Remaining = b.Remaining,
        HealthLeaveDays = b.HealthLeaveDays,
        AbsenceLimit = b.AbsenceLimit,
        UnexcusedAbsenceDays = b.UnexcusedAbsenceDays,
        AbsenceRemaining = b.AbsenceRemaining,
        HealthAbsenceDays = b.HealthAbsenceDays,
        ExcusedAbsenceDays = b.ExcusedAbsenceDays,
        Warnings = b.Warnings.Select(w => new WarningDto { Code = w.ToString(), Message = Message(w, b) }).ToList()
    };

    private static string Message(LeaveWarning warning, LeaveBalance b) => warning switch
    {
        LeaveWarning.LeaveNearLimit => $"İzin hakkı azalıyor: {b.Remaining:0.##} gün kaldı.",
        LeaveWarning.LeavePendingExceeds => "Bekleyen talepler onaylanırsa izin hakkı aşılacak.",
        LeaveWarning.LeaveLimitExceeded => $"İzin hakkı aşıldı ({b.Used:0.##}/{b.Limit} gün).",
        LeaveWarning.AbsenceNearLimit => $"Devamsızlık sınırına yaklaşıldı: {b.AbsenceRemaining:0.##} gün kaldı.",
        _ => $"Devamsızlık sınırı aşıldı ({b.UnexcusedAbsenceDays:0.##}/{b.AbsenceLimit} gün)."
    };
}
