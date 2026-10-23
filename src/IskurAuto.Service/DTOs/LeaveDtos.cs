using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

public class CreateLeaveRequest
{
    public LeaveType Type { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>Saatlik izinde, tek gün için izin süresi.</summary>
    [Range(0.5, 24)]
    public decimal? Hours { get; set; }

    [Required, StringLength(300, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class ReviewLeaveRequest
{
    public bool Approve { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}

public class LeaveDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public LeaveType Type { get; set; }
    public LeaveStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? Hours { get; set; }
    public decimal DaysCounted { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly AppliedOn { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public bool HasDocument { get; set; }
    public string? DocumentFileName { get; set; }
}

public class WarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class LeaveBalanceDto
{
    public int Year { get; set; }
    public int Limit { get; set; }
    public decimal Used { get; set; }

    /// <summary>Kullanılan günün izin olarak harcanan kısmı.</summary>
    public decimal LeaveDays { get; set; }

    /// <summary>Kullanılan günün devamsızlık olarak harcanan kısmı.</summary>
    public decimal AbsenceDays { get; set; }

    public decimal Pending { get; set; }
    public decimal Remaining { get; set; }
    public List<WarningDto> Warnings { get; set; } = new();
}

public class LeaveResultDto
{
    public LeaveDto Leave { get; set; } = null!;
    public LeaveBalanceDto Balance { get; set; } = null!;
}

public class LeaveAlertDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public LeaveBalanceDto Balance { get; set; } = null!;
}

public class LeaveSearchQuery
{
    public int? Year { get; set; }
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public LeaveStatus? Status { get; set; }
}

public class RecordAbsenceRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>İzin hakkından düşme açısından türler arasında fark yoktur; her devamsızlık günü 1 gün sayılır.</summary>
    public AbsenceType Type { get; set; } = AbsenceType.Unexcused;

    [StringLength(500)]
    public string? Note { get; set; }
}

public class RevokeAbsenceRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class AbsenceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public AbsenceType Type { get; set; }
    public string? Note { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByName { get; set; }
    public string? RevokeReason { get; set; }
}

public class AbsenceQuery
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
}

public class LeavePolicyDto
{
    [Range(0, 365)]
    public int LeaveDayLimit { get; set; }

    [Range(0, 365)]
    public int LeaveWarningRemainingDays { get; set; }

}

public sealed class DocumentUpload
{
    public Stream Content { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public long Length { get; init; }
}

public sealed class DocumentDownload
{
    public Stream Content { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
}
