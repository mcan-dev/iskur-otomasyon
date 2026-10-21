namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrencinin bir çalışma birimine atanma kaydı. Dekanlık/Sekreterlik tarafından yapılır.
/// Yeni atama yapıldığında eski kayıt silinmez, bitiş tarihi atanarak kapatılır (geçmiş korunur).
/// Aynı anda yalnızca bir açık atama olabilir (veritabanında filtreli benzersiz indeksle garanti edilir).
/// </summary>
public class StudentUnitAssignment : BaseEntity
{
    public int StudentProfileId { get; set; }
    public int WorkUnitId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Atamayı yapan kullanıcı (izlenebilirlik).</summary>
    public int AssignedByUserId { get; set; }

    public bool IsOpen => EndDate is null;

    public StudentProfile StudentProfile { get; set; } = null!;
    public WorkUnit WorkUnit { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
