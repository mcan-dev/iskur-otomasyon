using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

public class LeaveRequest : BaseEntity
{
    public int StudentProfileId { get; set; }

    public LeaveType Type { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>Saatlik izinde (tek gün) izin süresi; günlük izinde null.</summary>
    public decimal? Hours { get; set; }

    /// <summary>İzin hakkından düşülecek gün sayısı (yarım gün = 0,5).</summary>
    public decimal DaysCounted { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly AppliedOn { get; set; }

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public string? DocumentFileName { get; set; }

    public string? DocumentStoredName { get; set; }

    public string? DocumentContentType { get; set; }

    public bool HasDocument => DocumentStoredName is not null;

    public StudentProfile StudentProfile { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
