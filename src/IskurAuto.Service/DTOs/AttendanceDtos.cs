using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;
using IskurAuto.Service.Attendance;

namespace IskurAuto.Service.DTOs;

public class CreateSessionRequest
{
    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    [StringLength(150)]
    public string? Title { get; set; }

    /// <summary>Boşsa fakülte geneli. Bölüm başkanı için her zaman kendi bölümü kullanılır.</summary>
    public int? DepartmentId { get; set; }
}

public class SessionDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Title { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public bool IsActiveNow { get; set; }
}

public class QrCodeDto
{
    public int SessionId { get; set; }

    /// <summary>Karekoda yazılacak metin.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Bu zamandan sonra ekranın yeni karekod istemesi gerekir.</summary>
    public DateTime RefreshAtUtc { get; set; }

    public int WindowSeconds { get; set; }
}

public class CheckInRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ManualAttendanceRequest
{
    public int StudentId { get; set; }

    public int SessionId { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class VoidAttendanceRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class AttendanceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly SessionStart { get; set; }
    public TimeOnly SessionEnd { get; set; }
    public DateTime CheckedInAt { get; set; }
    public bool IsManual { get; set; }
    public string? ManualReason { get; set; }
    public string? CreatedByName { get; set; }
    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidedByName { get; set; }
    public string? VoidReason { get; set; }
}

public class AttendanceQuery
{
    public DateOnly? Date { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? SessionId { get; set; }
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public bool IncludeVoided { get; set; }
}

public class PlannedDayDto
{
    public DateOnly Date { get; set; }
    public PlannedDayOutcome Outcome { get; set; }
}

public class AttendanceComparisonDto
{
    public int StudentId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool HasApprovedPlan { get; set; }
    public decimal PlannedDays { get; set; }
    public decimal AttendedDays { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal RecordedAbsenceDays { get; set; }
    public decimal MissingDays { get; set; }
    public IReadOnlyList<PlannedDayDto> Days { get; set; } = Array.Empty<PlannedDayDto>();
}
