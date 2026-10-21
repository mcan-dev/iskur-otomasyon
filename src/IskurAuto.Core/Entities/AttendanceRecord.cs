namespace IskurAuto.Core.Entities;

/// <summary>
/// Bir öğrencinin bir oturumdaki devam kaydı. Hatalı kayıtlar silinmez, gerekçeyle iptal edilir;
/// elle eklenen kayıtlar da gerekçesi ve ekleyen kullanıcıyla saklanır.
/// </summary>
public class AttendanceRecord : BaseEntity
{
    public int StudentProfileId { get; set; }

    public int AttendanceSessionId { get; set; }

    /// <summary>Oturum tarihinin kopyası; günlük/aylık/yıllık sorgular için.</summary>
    public DateOnly Date { get; set; }

    public DateTime CheckedInAt { get; set; }

    /// <summary>Kayıt anında öğrencinin o gün için onaylı planındaki gün (karşılaştırma için).</summary>
    public int? WorkPlanDayId { get; set; }

    public bool IsManual { get; set; }

    public string? ManualReason { get; set; }

    /// <summary>Elle ekleyen kullanıcı; karekodla oluşan kayıtta null.</summary>
    public int? CreatedByUserId { get; set; }

    public DateTime? VoidedAt { get; set; }

    public int? VoidedByUserId { get; set; }

    public string? VoidReason { get; set; }

    public bool IsVoided => VoidedAt is not null;

    public StudentProfile StudentProfile { get; set; } = null!;
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public WorkPlanDay? WorkPlanDay { get; set; }
    public User? CreatedByUser { get; set; }
    public User? VoidedByUser { get; set; }
}
