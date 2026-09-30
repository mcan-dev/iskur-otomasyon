namespace IskurAuto.Core.Enums;

/// <summary>
/// Aylık puantaj kaydının İŞKUR otomasyon sürecindeki yaşam döngüsü statülerini tanımlar.
/// </summary>
public enum TimesheetStatus
{
    /// <summary>
    /// Fakülte sekreteri tarafından girildi; SKS Admin onayı bekleniyor.
    /// </summary>
    PendingSksApproval = 0,

    /// <summary>
    /// SKS Admin tarafından onaylandı; İŞKUR otomasyon kuyruğunda bekliyor.
    /// </summary>
    ApprovedBySks = 1,

    /// <summary>
    /// Playwright botu tarafından İŞKUR sistemine başarıyla işlendi.
    /// </summary>
    ProcessedToIskur = 2,

    /// <summary>
    /// Otomasyon süreci sırasında bir hata meydana geldi. Detaylar TaskLog tablosunda.
    /// </summary>
    Error = 3
}
