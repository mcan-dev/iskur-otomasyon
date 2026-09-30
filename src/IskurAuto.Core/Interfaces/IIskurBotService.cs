namespace IskurAuto.Core.Interfaces;

/// <summary>
/// İŞKUR E-Şube sistemine Playwright botu aracılığıyla otonom veri girişi yapan
/// servisin sözleşmesini (contract) tanımlar.
/// Yalnızca <c>IskurAuto.Automation</c> katmanı tarafından uygulanır.
/// </summary>
public interface IIskurBotService
{
    /// <summary>
    /// Onaylanmış (<c>ApprovedBySks</c>) aylık puantaj kaydını İŞKUR E-Şube sistemine girer.
    /// <list type="bullet">
    ///   <item>Playwright ile Chromium tarayıcısını görünür modda açar.</item>
    ///   <item>İŞKUR E-Şube sistemine giriş yapar ve puantajı işler.</item>
    ///   <item>Başarıda statüyü <c>ProcessedToIskur</c>, hata durumunda <c>Error</c> olarak günceller.</item>
    ///   <item>Her adımı <c>TaskLog</c> tablosuna kaydeder.</item>
    /// </list>
    /// </summary>
    /// <param name="timesheetId">İşlenecek <c>MonthlyTimesheet</c> kaydının ID'si.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılabilir.</param>
    Task RunTimesheetEntryAsync(int timesheetId, CancellationToken cancellationToken = default);
}
