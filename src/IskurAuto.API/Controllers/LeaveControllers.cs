using IskurAuto.Core.Constants;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>İzin talepleri ve izin hakkı.</summary>
[ApiController]
[Route("api/leaves")]
[Produces("application/json")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaves;

    public LeavesController(ILeaveService leaves) => _leaves = leaves;

    [HttpGet("me/balance")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<LeaveBalanceDto>> MyBalance([FromQuery] int year, CancellationToken ct)
        => Ok(await _leaves.GetMyBalanceAsync(year, ct));

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyList<LeaveDto>>> Mine([FromQuery] int? year, CancellationToken ct)
        => Ok(await _leaves.ListMineAsync(year, ct));

    /// <summary>İzin seçimi için ayın takvimi: yalnızca onaylı plandaki, geçmemiş çalışma günleri seçilebilir.</summary>
    [HttpGet("me/calendar")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<LeaveCalendarDto>> MyCalendar([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await _leaves.GetMyCalendarAsync(year, month, ct));

    /// <summary>Seçilen çalışma günleri için izin alır. İzin onay beklemez; sekreterlik bilgilendirilir.</summary>
    [HttpPost("me")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<LeaveDto>>> Request([FromBody] CreateLeaveRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _leaves.RequestAsync(request, ct));

    [HttpPost("me/{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<LeaveDto>> CancelMine(int id, CancellationToken ct)
        => Ok(await _leaves.CancelMineAsync(id, ct));

    /// <summary>Öğrencinin izinleri; öğrencinin sorumlusu olan herkes (sorumlu personel, birim yetkilisi, sekreterlik) görür.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<IReadOnlyList<LeaveDto>>> Search([FromQuery] LeaveSearchQuery query, CancellationToken ct)
        => Ok(await _leaves.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveDto>> GetById(int id, CancellationToken ct)
        => Ok(await _leaves.GetByIdAsync(id, ct));

    /// <summary>İzni iptal eder. Yalnızca fakülte sekreterliği; izin onaylama adımı yoktur.</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<LeaveDto>> Cancel(int id, [FromBody] CancelLeaveRequest request, CancellationToken ct)
        => Ok(await _leaves.CancelAsync(id, request, ct));

    [HttpGet("students/{studentId:int}/balance")]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<LeaveBalanceDto>> StudentBalance(int studentId, [FromQuery] int year, CancellationToken ct)
        => Ok(await _leaves.GetStudentBalanceAsync(studentId, year, ct));

    /// <summary>İzin veya devamsızlık sınırına yaklaşan ya da sınırı aşan öğrenciler.</summary>
    [HttpGet("alerts")]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<IReadOnlyList<LeaveAlertDto>>> Alerts([FromQuery] int year, CancellationToken ct)
        => Ok(await _leaves.GetAlertsAsync(year, ct));
}

/// <summary>Devamsızlık kayıtları.</summary>
[ApiController]
[Route("api/absences")]
[Produces("application/json")]
[Authorize]
public class AbsencesController : ControllerBase
{
    private readonly IAbsenceService _absences;

    public AbsencesController(IAbsenceService absences) => _absences = absences;

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyList<AbsenceDto>>> Mine([FromQuery] int? year, CancellationToken ct)
        => Ok(await _absences.ListMineAsync(year, ct));

    [HttpGet]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<IReadOnlyList<AbsenceDto>>> Search([FromQuery] AbsenceQuery query, CancellationToken ct)
        => Ok(await _absences.SearchAsync(query, ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<AbsenceDto>> Record([FromBody] RecordAbsenceRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _absences.RecordAsync(request, ct));

    /// <summary>Hatalı kaydı gerekçesiyle geri alır; kayıt silinmez.</summary>
    [HttpPost("{id:int}/revoke")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<AbsenceDto>> Revoke(int id, [FromBody] RevokeAbsenceRequest request, CancellationToken ct)
        => Ok(await _absences.RevokeAsync(id, request, ct));
}

/// <summary>İzin ve devamsızlık sınırları.</summary>
[ApiController]
[Route("api/settings/leave")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.AllStaff)]
public class LeaveSettingsController : ControllerBase
{
    private readonly ILeaveSettingsService _settings;

    public LeaveSettingsController(ILeaveSettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<ActionResult<LeavePolicyDto>> Get(CancellationToken ct)
        => Ok(await _settings.GetAsync(ct));

    [HttpPut]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<LeavePolicyDto>> Update([FromBody] LeavePolicyDto request, CancellationToken ct)
        => Ok(await _settings.UpdateAsync(request, ct));
}
