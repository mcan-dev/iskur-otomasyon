namespace IskurAuto.Core.Entities;

/// <summary>
/// Playwright botunun İŞKUR sistemine puantaj girerken oluşturduğu
/// denetim (audit) ve hata izlerini (loglarını) tutar.
/// Her kayıt, otomasyon sürecinin bir adımını temsil eder.
/// </summary>
public class TaskLog
{
    public int Id { get; set; }

    /// <summary>Gerçekleştirilen otomasyon adımının açıklaması. Örn: "Navigating to ISKUR login page"</summary>
    public string StepDescription { get; set; } = string.Empty;

    /// <summary>Adımın başarıyla tamamlanıp tamamlanmadığını gösterir.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Hata durumunda Playwright'in veya sistemin ürettiği hata mesajı.</summary>
    public string? ErrorDetail { get; set; }

    /// <summary>Hatanın tam stack trace'i (debug için).</summary>
    public string? StackTrace { get; set; }

    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int MonthlyTimesheetId { get; set; }

    // Navigation Properties
    public MonthlyTimesheet MonthlyTimesheet { get; set; } = null!;
}
