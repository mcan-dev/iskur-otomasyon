namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrenciye ait kişisel ve akademik bilgiler (belge madde 3).
/// Programa katılım geçmişi, birim ve danışman atamaları ayrı tablolardadır; böylece
/// "geçmiş kayıtlar kaybolmadan incelenebilir" ilkesi sağlanır (belge madde 9).
/// </summary>
public class StudentProfile : BaseEntity
{
    public int UserId { get; set; }

    /// <summary>T.C. Kimlik No (11 hane, algoritmayla doğrulanmış, benzersiz).</summary>
    public string NationalId { get; set; } = string.Empty;

    /// <summary>IBAN (boşluksuz, büyük harf; "TR" + 24 hane).</summary>
    public string Iban { get; set; } = string.Empty;

    /// <summary>Üniversite öğrenci numarası (benzersiz).</summary>
    public string StudentNumber { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    /// <summary>Kayıt yılı (örn. 2026).</summary>
    public int RegistrationYear { get; set; }

    /// <summary>
    /// Öğrenciye özel yıllık izin hakkı (gün). Boşsa genel izin hakkı geçerlidir.
    /// Fakülte sekreterliği tarafından değiştirilir.
    /// </summary>
    public int? LeaveDayAllowance { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public ICollection<ProgramParticipation> Participations { get; set; } = new List<ProgramParticipation>();
    public ICollection<StudentUnitAssignment> UnitAssignments { get; set; } = new List<StudentUnitAssignment>();
    public ICollection<AdvisorAssignment> AdvisorAssignments { get; set; } = new List<AdvisorAssignment>();

    // ── Türetilmiş (veritabanına yazılmayan) yardımcılar ──
    // Not: Bunlar ilgili koleksiyonlar yüklenmişse (Include) doğru sonuç verir.

    /// <summary>Şu an açık (bitiş tarihi olmayan) program katılımı; yoksa öğrenci pasiftir.</summary>
    public ProgramParticipation? CurrentParticipation => Participations.FirstOrDefault(p => p.IsOpen);

    /// <summary>Aktif/Pasif durumu = açık bir katılım dönemi var mı?</summary>
    public bool IsActive => CurrentParticipation is not null;

    public StudentUnitAssignment? CurrentUnitAssignment => UnitAssignments.FirstOrDefault(a => a.IsOpen);

    public AdvisorAssignment? CurrentAdvisorAssignment => AdvisorAssignments.FirstOrDefault(a => a.IsOpen);
}
