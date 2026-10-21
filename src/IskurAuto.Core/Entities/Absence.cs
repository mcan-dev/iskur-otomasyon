using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

public class Absence : BaseEntity
{
    public int StudentProfileId { get; set; }

    public DateOnly Date { get; set; }

    public AbsenceType Type { get; set; }

    public string? Note { get; set; }

    public int RecordedByUserId { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? RevokedByUserId { get; set; }

    public string? RevokeReason { get; set; }

    public bool IsRevoked => RevokedAt is not null;

    public StudentProfile StudentProfile { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
    public User? RevokedByUser { get; set; }
}
