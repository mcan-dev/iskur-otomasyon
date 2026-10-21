namespace IskurAuto.Core.Entities;

/// <summary>
/// Yetkili kullanıcının belirlediği çalışma günü ve saat aralığı. Bu aralıkta ekranda
/// 30 saniyede bir yenilenen karekod gösterilir.
/// </summary>
public class AttendanceSession : BaseEntity
{
    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Title { get; set; }

    /// <summary>null ise fakülte genelinde; doluysa yalnızca o bölümün öğrencileri okutabilir.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Karekod imzası için oturuma özel gizli anahtar. Hiçbir yanıtta dışarı verilmez.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public DateTime? ClosedAt { get; set; }

    public bool IsClosed => ClosedAt is not null;

    public Department? Department { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
