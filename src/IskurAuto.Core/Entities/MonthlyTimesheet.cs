using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>
/// Bir öğrencinin belirli bir aya ait çalışma saatlerini (puantajını) temsil eder.
/// Maker-Checker iş akışının merkezi varlığıdır; Status alanı ile süreç takibi yapılır.
/// </summary>
public class MonthlyTimesheet
{
    public int Id { get; set; }

    /// <summary>Puantajın ait olduğu yıl. Örn: 2025</summary>
    public int Year { get; set; }

    /// <summary>Puantajın ait olduğu ay (1-12).</summary>
    public int Month { get; set; }

    /// <summary>İlgili ayda çalışılan toplam saat sayısı.</summary>
    public decimal TotalHoursWorked { get; set; }

    /// <summary>
    /// Puantajın mevcut durumu.
    /// Maker-Checker iş akışı bu alan üzerinden yönetilir.
    /// </summary>
    public TimesheetStatus Status { get; set; } = TimesheetStatus.PendingSksApproval;

    /// <summary>Fakülte Sekreteri'nin (Maker) kayıt notu (isteğe bağlı).</summary>
    public string? MakerNote { get; set; }

    /// <summary>SKS Admin'in (Checker) onay veya red gerekçesi (isteğe bağlı).</summary>
    public string? CheckerNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    // Foreign Keys
    public int StudentWorkerId { get; set; }

    /// <summary>Onaylayan SKS Admin kullanıcısının ID'si.</summary>
    public int? ApprovedByUserId { get; set; }

    // Navigation Properties
    public StudentWorker StudentWorker { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ICollection<TaskLog> TaskLogs { get; set; } = new List<TaskLog>();
}
