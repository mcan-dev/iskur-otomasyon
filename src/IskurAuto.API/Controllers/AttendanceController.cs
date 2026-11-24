using IskurAuto.Core.Constants;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Karekodla devam takibi.</summary>
[ApiController]
[Route("api/attendance")]
[Produces("application/json")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private const string SessionManagers = AppRoles.FacultyAdmin;   // karekod ve oturumları yalnızca fakülte sekreterliği yönetir

    private readonly IAttendanceService _attendance;
    private readonly IAttendanceDisplayService _display;

    public AttendanceController(IAttendanceService attendance, IAttendanceDisplayService display)
    {
        _attendance = attendance;
        _display = display;
    }

    // Mesai karekodu (monitör ekranı)

    /// <summary>Sabah ve akşam karekod saatleri.</summary>
    [HttpGet("schedule")]
    public async Task<ActionResult<AttendanceScheduleDto>> Schedule(CancellationToken ct)
        => Ok(await _display.GetScheduleAsync(ct));

    /// <summary>Sabah ve akşam karekod saatlerini belirler (yalnızca fakülte sekreterliği).</summary>
    [HttpPut("schedule")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceScheduleDto>> UpdateSchedule([FromBody] AttendanceScheduleDto request, CancellationToken ct)
        => Ok(await _display.UpdateScheduleAsync(request, ct));

    /// <summary>
    /// Monitör ekranı: karekod saatleri içindeyse güncel karekodu (30 saniyede bir yenilenir), değilse sıradaki saati döner.
    /// </summary>
    [HttpGet("display")]
    [Authorize(Roles = SessionManagers)]
    public async Task<ActionResult<AttendanceDisplayDto>> Display(CancellationToken ct)
        => Ok(await _display.GetDisplayAsync(ct));

    // Oturum ve karekod

    [HttpPost("sessions")]
    [Authorize(Roles = SessionManagers)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionDto>> CreateSession([FromBody] CreateSessionRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _attendance.CreateSessionAsync(request, ct));

    [HttpGet("sessions")]
    [Authorize(Roles = SessionManagers)]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> Sessions(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await _attendance.ListSessionsAsync(from, to, ct));

    [HttpPost("sessions/{id:int}/close")]
    [Authorize(Roles = SessionManagers)]
    public async Task<ActionResult<SessionDto>> CloseSession(int id, CancellationToken ct)
        => Ok(await _attendance.CloseSessionAsync(id, ct));

    /// <summary>Ekranda gösterilecek güncel karekod. <c>refreshAtUtc</c> geçince yeniden istenmelidir.</summary>
    [HttpGet("sessions/{id:int}/qr")]
    [Authorize(Roles = SessionManagers)]
    public async Task<ActionResult<QrCodeDto>> CurrentQr(int id, CancellationToken ct)
        => Ok(await _attendance.GetCurrentQrAsync(id, ct));

    // Öğrenci

    [HttpPost("check-in")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceDto>> CheckIn([FromBody] CheckInRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _attendance.CheckInAsync(request, ct));

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyList<AttendanceDto>>> Mine(
        [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await _attendance.ListMineAsync(year, month, ct));

    [HttpGet("me/comparison")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<AttendanceComparisonDto>> MyComparison(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await _attendance.GetMyComparisonAsync(year, month, ct));

    // Yetkili kullanıcılar

    /// <summary>Günlük (date), aylık (year+month) veya yıllık (year) devam kayıtları.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<IReadOnlyList<AttendanceDto>>> Search([FromQuery] AttendanceQuery query, CancellationToken ct)
        => Ok(await _attendance.SearchAsync(query, ct));

    /// <summary>Planlanan çalışma günleri ile gerçekleşen devamın karşılaştırması.</summary>
    [HttpGet("students/{studentId:int}/comparison")]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<AttendanceComparisonDto>> Comparison(
        int studentId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await _attendance.GetComparisonAsync(studentId, year, month, ct));

    /// <summary>Okutamayan öğrenci için gerekçeli elle kayıt.</summary>
    [HttpPost("manual")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<AttendanceDto>> AddManually([FromBody] ManualAttendanceRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _attendance.AddManuallyAsync(request, ct));

    /// <summary>Hatalı kaydı gerekçeyle iptal eder (kayıt silinmez).</summary>
    [HttpPost("{id:int}/void")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<AttendanceDto>> Void(int id, [FromBody] VoidAttendanceRequest request, CancellationToken ct)
        => Ok(await _attendance.VoidAsync(id, request, ct));
}
