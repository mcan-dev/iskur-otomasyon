namespace IskurAuto.Core.Models;

/// <summary>İzin ve devamsızlık sınırları. Yetkili kullanıcılar tarafından veritabanından değiştirilir.</summary>
public class LeavePolicy
{
    public int LeaveDayLimit { get; set; } = 10;

    /// <summary>Kalan izin bu değere veya altına inince uyarı verilir.</summary>
    public int LeaveWarningRemainingDays { get; set; } = 2;

    public int AbsenceDayLimit { get; set; } = 5;

    public int AbsenceWarningRemainingDays { get; set; } = 1;

    /// <summary>Sağlık raporlu izinler izin hakkından düşülsün mü?</summary>
    public bool HealthLeaveCountsTowardLimit { get; set; }
}
