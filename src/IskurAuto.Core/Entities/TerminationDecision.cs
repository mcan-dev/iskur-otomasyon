using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrencinin programdan ilişiğinin kesilmesine dair karar kaydı. Karar verildiği andaki
/// izin ve devamsızlık durumu kayda alınır; kayıt sonradan değiştirilmez veya silinmez.
/// </summary>
public class TerminationDecision : BaseEntity
{
    public int StudentProfileId { get; set; }

    public TerminationBasis Basis { get; set; }

    public DateOnly DecisionDate { get; set; }

    public string Note { get; set; } = string.Empty;

    /// <summary>Karar günündeki yıl için onaylı izin günü.</summary>
    public decimal LeaveDaysUsed { get; set; }

    /// <summary>Karar günündeki yıl için mazeretsiz devamsızlık günü.</summary>
    public int UnexcusedAbsenceDays { get; set; }

    public int DecidedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public User DecidedByUser { get; set; } = null!;
}
