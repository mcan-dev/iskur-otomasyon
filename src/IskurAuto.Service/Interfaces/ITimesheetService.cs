using IskurAuto.Service.DTOs;

namespace IskurAuto.Service.Interfaces;

/// <summary>
/// Aylık puantaj (timesheet) iş kurallarını ve Maker-Checker iş akışını
/// tanımlayan servis sözleşmesi (contract).
/// </summary>
public interface ITimesheetService
{
    // ─── Maker (Fakülte Sekreteri) Operasyonları ─────────────────────────────

    /// <summary>
    /// Fakülte Sekreteri tarafından yeni bir puantaj kaydı oluşturur.
    /// Status otomatik olarak <c>PendingSksApproval</c> atanır.
    /// </summary>
    /// <param name="dto">Puantaj bilgileri.</param>
    /// <param name="makerUserId">İşlemi yapan sekreterin kullanıcı ID'si (yetki kontrolü için).</param>
    /// <returns>Oluşturulan puantajın sonuç DTO'su.</returns>
    Task<TimesheetResultDto> CreateTimesheetAsync(TimesheetCreateDto dto, int makerUserId);

    // ─── Checker (SKS Admin) Operasyonları ───────────────────────────────────

    /// <summary>
    /// SKS Admin tarafından bekleyen bir puantajı onaylar veya reddeder.
    /// Onaylanırsa Status → <c>ApprovedBySks</c> olur ve otomasyon tetiklenir.
    /// Reddedilirse Status → <c>PendingSksApproval</c>'a döner.
    /// </summary>
    /// <param name="dto">Onay/red kararı ve gerekçe.</param>
    /// <param name="checkerUserId">Onaylayan SKS Admin'in kullanıcı ID'si.</param>
    /// <returns>Güncellenmiş puantajın sonuç DTO'su.</returns>
    Task<TimesheetResultDto> ApproveOrRejectTimesheetAsync(TimesheetApproveDto dto, int checkerUserId);

    // ─── Sorgulama Operasyonları ──────────────────────────────────────────────

    /// <summary>Belirli bir puantajı ID'ye göre getirir.</summary>
    Task<TimesheetResultDto?> GetTimesheetByIdAsync(int timesheetId);

    /// <summary>
    /// Fakültey özgü tüm puantaj kayıtlarını getirir (Multi-Tenant izolasyonu).
    /// </summary>
    Task<IEnumerable<TimesheetResultDto>> GetTimesheetsByFacultyAsync(int facultyId);

    /// <summary>SKS Admin'in onayını bekleyen tüm puantajları getirir.</summary>
    Task<IEnumerable<TimesheetResultDto>> GetPendingTimesheetsAsync();
}
