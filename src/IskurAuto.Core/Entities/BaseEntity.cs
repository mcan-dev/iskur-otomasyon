namespace IskurAuto.Core.Entities;

/// <summary>
/// Tüm tabloların ortak alanlarını taşıyan taban sınıf.
/// Belge ilkesi: "Her öğrenciye ve her kayda benzersiz bir kimlik verilmelidir."
/// CreatedAt / UpdatedAt alanları DbContext tarafından otomatik doldurulur;
/// böylece servis katmanı bu teknik ayrıntıyla uğraşmaz (Single Responsibility).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    /// <summary>Kaydın oluşturulma zamanı (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Kaydın son değiştirilme zamanı (UTC). Hiç değişmediyse null.</summary>
    public DateTime? UpdatedAt { get; set; }
}
