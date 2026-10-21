namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrenciye danışman öğretim elemanı atama kaydı. Bölüm Başkanı (veya Fakülte yönetimi) yapar.
/// Danışman değiştiğinde eski kayıt kapatılır, silinmez: "danışmanı değiştiğinde geçmiş
/// danışmanlık kayıtları kaybolmamalıdır" (belge madde 9).
/// </summary>
public class AdvisorAssignment : BaseEntity
{
    public int StudentProfileId { get; set; }

    /// <summary>Danışman rolündeki kullanıcının Id'si.</summary>
    public int AdvisorUserId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public int AssignedByUserId { get; set; }

    public bool IsOpen => EndDate is null;

    public StudentProfile StudentProfile { get; set; } = null!;
    public User AdvisorUser { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
