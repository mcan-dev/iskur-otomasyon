namespace IskurAuto.Core.Entities;

/// <summary>Danışmanın bir öğrencinin devamsızlığını bölüm başkanına bildirmesi.</summary>
public class AbsenceNotice : BaseEntity
{
    public int StudentProfileId { get; set; }

    /// <summary>Devamsızlık yapılan gün.</summary>
    public DateOnly Date { get; set; }

    public string Message { get; set; } = string.Empty;

    public int AdvisorUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public int? ReadByUserId { get; set; }

    public bool IsRead => ReadAt is not null;

    public StudentProfile StudentProfile { get; set; } = null!;
    public User AdvisorUser { get; set; } = null!;
    public User? ReadByUser { get; set; }
}
