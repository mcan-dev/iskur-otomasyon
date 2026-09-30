using System.ComponentModel.DataAnnotations;

namespace IskurAuto.Service.DTOs;

/// <summary>
/// Fakülte Sekreteri'nin (Maker) yeni bir aylık puantaj kaydı oluştururken
/// API'ye göndereceği veri transfer nesnesi.
/// Status alanı burada yer almaz; servis katmanı tarafından
/// otomatik olarak <c>PendingSksApproval</c> atanır.
/// </summary>
public class TimesheetCreateDto
{
    /// <summary>Puantajı girilecek öğrencinin sistem ID'si.</summary>
    [Required]
    public int StudentWorkerId { get; set; }

    /// <summary>Puantajın ait olduğu yıl. Örn: 2025</summary>
    [Required]
    [Range(2020, 2100, ErrorMessage = "Yıl 2020-2100 aralığında olmalıdır.")]
    public int Year { get; set; }

    /// <summary>Puantajın ait olduğu ay (1-12).</summary>
    [Required]
    [Range(1, 12, ErrorMessage = "Ay 1-12 arasında bir değer olmalıdır.")]
    public int Month { get; set; }

    /// <summary>İlgili ayda öğrencinin çalıştığı toplam saat sayısı.</summary>
    [Required]
    [Range(0.5, 999.99, ErrorMessage = "Çalışılan saat 0.5 ile 999.99 arasında olmalıdır.")]
    public decimal TotalHoursWorked { get; set; }

    /// <summary>Fakülte Sekreteri'nin isteğe bağlı açıklama notu.</summary>
    [MaxLength(1000)]
    public string? MakerNote { get; set; }
}
