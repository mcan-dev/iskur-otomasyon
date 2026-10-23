namespace IskurAuto.Core.Models;

/// <summary>
/// İzin hakkı. Tek bir hak vardır: onaylı izin günleri ve kayıtlı devamsızlık günleri aynı haktan düşer.
/// Yetkili kullanıcılar tarafından değiştirilir; öğrenciye özel hak da tanımlanabilir.
/// </summary>
public class LeavePolicy
{
    public int LeaveDayLimit { get; set; } = 10;

    /// <summary>Kalan hak bu değere veya altına inince uyarı verilir.</summary>
    public int LeaveWarningRemainingDays { get; set; } = 2;
}
