using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

/// <summary>
/// İzin talebi: öğrenci aynı ayın onaylı çalışma planından bir veya birden çok gün seçer.
/// Tür ve gerekçe yoktur; açıklama isteğe bağlıdır. Talep anında geçerli olur.
/// </summary>
public class CreateLeaveRequest
{
    [Required, MinLength(1)]
    public List<DateOnly> Days { get; set; } = new();

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class CancelLeaveRequest
{
    [StringLength(1000)]
    public string? Note { get; set; }
}

public class LeaveDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysCounted { get; set; }
    public string? Description { get; set; }
    public DateOnly AppliedOn { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelNote { get; set; }
}

/// <summary>Öğrencinin izin seçebileceği takvim: o ayın onaylı plandaki çalışma günleri.</summary>
public class LeaveCalendarDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public bool HasApprovedPlan { get; set; }
    public List<LeaveCalendarDayDto> Days { get; set; } = new();
    public LeaveBalanceDto Balance { get; set; } = null!;
}

public class LeaveCalendarDayDto
{
    public DateOnly Date { get; set; }

    /// <summary>Onaylı plandaki çalışma günü mü?</summary>
    public bool IsWorkDay { get; set; }

    /// <summary>Bu gün için geçerli izin var mı?</summary>
    public bool IsOnLeave { get; set; }

    /// <summary>İzin için seçilebilir mi (çalışma günü, geçmemiş, izinli değil).</summary>
    public bool CanSelect { get; set; }
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

    public decimal Remaining { get; set; }
    public List<WarningDto> Warnings { get; set; } = new();
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
    public int? Month { get; set; }
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
