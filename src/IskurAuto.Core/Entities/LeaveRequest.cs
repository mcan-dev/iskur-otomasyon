using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrencinin bir çalışma günü için aldığı izin. Öğrenci takvimden birden çok gün seçtiğinde her gün için
/// ayrı bir kayıt açılır; böylece sekreterlik tek bir günü ayrıca iptal edebilir.
/// İzin onay beklemez: talep edildiği anda geçerlidir, sekreterlik yalnızca bilgilendirilir.
/// </summary>
public class LeaveRequest : BaseEntity
{
    public int StudentProfileId { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Approved;

    /// <summary>İzinli gün. Tek günlük kayıtlarda <see cref="EndDate"/> ile aynıdır.</summary>
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>İzin hakkından düşülen gün sayısı (çalışma günü başına 1).</summary>
    public decimal DaysCounted { get; set; }

    /// <summary>İsteğe bağlı açıklama.</summary>
    public string? Description { get; set; }

    public DateOnly AppliedOn { get; set; }

    /// <summary>İzni iptal eden kullanıcı (öğrencinin kendisi veya sekreterlik).</summary>
    public int? CancelledByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelNote { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public User? CancelledByUser { get; set; }
}
