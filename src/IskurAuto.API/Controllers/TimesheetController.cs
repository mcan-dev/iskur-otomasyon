using IskurAuto.API.Infrastructure;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>
/// Aylık puantaj (timesheet) yönetim uç noktaları.
/// Tüm endpoint'ler JWT ile korunmaktadır.
/// Kullanıcı kimliği ve fakülte bilgisi istek gövdesinden değil,
/// doğrudan JWT token içindeki claim'lerden güvenli biçimde okunur.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // ← Tüm controller JWT koruması altında
public class TimesheetController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;
    private readonly ILogger<TimesheetController> _logger;

    public TimesheetController(
        ITimesheetService timesheetService,
        ILogger<TimesheetController> logger)
    {
        _timesheetService = timesheetService
            ?? throw new ArgumentNullException(nameof(timesheetService));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── Yardımcı: Claim'lerden Güvenli Okuma ────────────────────────────────

    /// <summary>
    /// JWT token'ından kullanıcı ID'sini güvenli biçimde okur.
    /// Token geçerli ama claim eksikse 401 döndürülür.
    /// </summary>
    private bool TryGetUserIdFromClaims(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(AppClaimTypes.UserId)?.Value;
        return claim is not null && int.TryParse(claim, out userId);
    }

    /// <summary>
    /// JWT token'ından fakülte ID'sini güvenli biçimde okur.
    /// SKS Admin gibi tüm fakültelere yetkili kullanıcılar için null dönebilir.
    /// </summary>
    private int? GetFacultyIdFromClaims()
    {
        var claim = User.FindFirst(AppClaimTypes.FacultyId)?.Value;
        return claim is not null && int.TryParse(claim, out var facultyId)
            ? facultyId
            : null;
    }

    // ─── Maker (Fakülte Sekreteri) Uç Noktaları ──────────────────────────────

    /// <summary>
    /// [MAKER] Yeni bir aylık puantaj kaydı oluşturur.
    /// Yalnızca <c>FacultySecretary</c> rolüne sahip kullanıcılar erişebilir.
    /// Status otomatik olarak <c>PendingSksApproval</c> atanır.
    /// Kullanıcı ve fakülte bilgisi JWT claim'lerinden okunur; DTO'dan gelmez.
    /// </summary>
    /// <param name="dto">Puantaj bilgileri (öğrenci ID, ay, yıl, toplam saat).</param>
    /// <response code="201">Puantaj başarıyla oluşturuldu.</response>
    /// <response code="400">Geçersiz giriş verisi veya iş kuralı ihlali.</response>
    /// <response code="401">Geçersiz veya eksik JWT token.</response>
    /// <response code="403">Kullanıcı FacultySecretary rolüne sahip değil.</response>
    [HttpPost]
    [Authorize(Roles = "FacultySecretary")] // ← Yalnızca Maker rolü
    [ProducesResponseType(typeof(TimesheetResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTimesheet([FromBody] TimesheetCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Kullanıcı ID'sini JWT claim'inden güvenli oku — DTO'dan değil
        if (!TryGetUserIdFromClaims(out var makerUserId))
        {
            _logger.LogWarning("JWT token'ında geçerli 'uid' claim'i bulunamadı.");
            return Unauthorized(new ProblemDetails
            {
                Title  = "Kimlik Doğrulama Hatası",
                Detail = "Token içinde geçerli kullanıcı kimliği bulunamadı.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _timesheetService.CreateTimesheetAsync(dto, makerUserId);
            return CreatedAtAction(nameof(GetTimesheetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tenant izolasyon ihlali — MakerUserId={UserId}", makerUserId);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title  = "Erişim Reddedildi",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "İş kuralı ihlali — CreateTimesheet");
            return BadRequest(new ProblemDetails
            {
                Title  = "Geçersiz İşlem",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // ─── Checker (SKS Admin) Uç Noktaları ────────────────────────────────────

    /// <summary>
    /// [CHECKER] Bekleyen bir puantaj kaydını onaylar veya reddeder.
    /// Yalnızca <c>SksAdmin</c> rolüne sahip kullanıcılar erişebilir.
    /// Onaylayan kullanıcı bilgisi JWT claim'lerinden okunur; DTO'dan gelmez.
    /// </summary>
    /// <param name="dto">Onay/red kararı ve gerekçe notu.</param>
    /// <response code="200">Onay/red işlemi başarıyla tamamlandı.</response>
    /// <response code="400">Geçersiz giriş verisi veya iş kuralı ihlali.</response>
    /// <response code="401">Geçersiz veya eksik JWT token.</response>
    /// <response code="403">Kullanıcı SksAdmin rolüne sahip değil.</response>
    [HttpPut("approve")]
    [Authorize(Roles = "SksAdmin")] // ← Yalnızca Checker rolü
    [ProducesResponseType(typeof(TimesheetResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveOrRejectTimesheet([FromBody] TimesheetApproveDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Checker ID'sini JWT claim'inden güvenli oku — DTO'dan değil
        if (!TryGetUserIdFromClaims(out var checkerUserId))
        {
            _logger.LogWarning("JWT token'ında geçerli 'uid' claim'i bulunamadı.");
            return Unauthorized(new ProblemDetails
            {
                Title  = "Kimlik Doğrulama Hatası",
                Detail = "Token içinde geçerli kullanıcı kimliği bulunamadı.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var result = await _timesheetService.ApproveOrRejectTimesheetAsync(dto, checkerUserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Yetki hatası — CheckerUserId={UserId}", checkerUserId);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title  = "Erişim Reddedildi",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "İş kuralı ihlali — ApproveOrRejectTimesheet");
            return BadRequest(new ProblemDetails
            {
                Title  = "Geçersiz İşlem",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // ─── Sorgulama Uç Noktaları ───────────────────────────────────────────────

    /// <summary>
    /// Belirtilen ID'ye sahip puantaj kaydını getirir.
    /// Tüm yetkili kullanıcılar erişebilir.
    /// </summary>
    /// <param name="id">Puantaj ID'si.</param>
    /// <response code="200">Puantaj kaydı bulundu.</response>
    /// <response code="404">Belirtilen ID'ye sahip kayıt bulunamadı.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TimesheetResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimesheetById(int id)
    {
        var result = await _timesheetService.GetTimesheetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Belirli bir fakülteye ait tüm puantaj kayıtlarını getirir.
    /// <c>FacultySecretary</c> rolündeki kullanıcı yalnızca kendi fakültesinin
    /// ID'sini sorgulayabilir; <c>SksAdmin</c> tüm fakülteleri görebilir.
    /// </summary>
    /// <param name="facultyId">Fakülte ID'si (Tenant kimliği).</param>
    /// <response code="200">Puantaj kayıtları listelendi.</response>
    /// <response code="403">Kendi fakültesi dışında sorgulama yetkisi yok.</response>
    [HttpGet("faculty/{facultyId:int}")]
    [ProducesResponseType(typeof(IEnumerable<TimesheetResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTimesheetsByFaculty(int facultyId)
    {
        // Tenant izolasyonu: FacultySecretary yalnızca kendi fakültesini sorgulayabilir
        var claimFacultyId = GetFacultyIdFromClaims();
        var isSecretary    = User.IsInRole("FacultySecretary");

        if (isSecretary && claimFacultyId.HasValue && claimFacultyId.Value != facultyId)
        {
            _logger.LogWarning(
                "Tenant ihlali girişimi — UserId={UserId}, İstenen FacultyId={Requested}, Token FacultyId={Token}",
                User.FindFirst(AppClaimTypes.UserId)?.Value, facultyId, claimFacultyId);

            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title  = "Erişim Reddedildi",
                Detail = "Yalnızca kendi fakültenizin verilerini sorgulayabilirsiniz.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var results = await _timesheetService.GetTimesheetsByFacultyAsync(facultyId);
        return Ok(results);
    }

    /// <summary>
    /// SKS Admin onayını bekleyen tüm puantaj kayıtlarını getirir.
    /// Yalnızca <c>SksAdmin</c> rolüne sahip kullanıcılar erişebilir.
    /// </summary>
    /// <response code="200">Bekleyen puantaj kayıtları listelendi.</response>
    [HttpGet("pending")]
    [Authorize(Roles = "SksAdmin")]
    [ProducesResponseType(typeof(IEnumerable<TimesheetResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingTimesheets()
    {
        var results = await _timesheetService.GetPendingTimesheetsAsync();
        return Ok(results);
    }
}
