using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Öğrenci görevleri ve danışman değerlendirmeleri.</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    [HttpPost]
    [Authorize(Roles = AppRoles.AllStaff)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Assign([FromBody] AssignTaskRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _tasks.AssignAsync(request, ct));

    [HttpGet]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> Search([FromQuery] TaskQuery query, CancellationToken ct)
        => Ok(await _tasks.SearchAsync(query, ct));

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> Mine([FromQuery] StudentTaskStatus? status, CancellationToken ct)
        => Ok(await _tasks.ListMineAsync(status, ct));

    /// <summary>Görev ve tüm değerlendirme geçmişi.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> GetById(int id, CancellationToken ct)
        => Ok(await _tasks.GetByIdAsync(id, ct));

    /// <summary>Danışman not ekler ve/veya görev durumunu günceller. Önceki kayıtlar korunur.</summary>
    [HttpPost("{id:int}/evaluations")]
    [Authorize(Roles = AppRoles.Advisor)]
    public async Task<ActionResult<TaskDto>> Evaluate(int id, [FromBody] EvaluateTaskRequest request, CancellationToken ct)
        => Ok(await _tasks.EvaluateAsync(id, request, ct));
}

/// <summary>Danışmanın bölüm başkanına ilettiği devamsızlık bildirimleri.</summary>
[ApiController]
[Route("api/absence-notices")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.AllStaff)]
public class AbsenceNoticesController : ControllerBase
{
    private readonly IAbsenceNoticeService _notices;

    public AbsenceNoticesController(IAbsenceNoticeService notices) => _notices = notices;

    [HttpPost]
    [Authorize(Roles = AppRoles.Advisor)]
    public async Task<ActionResult<AbsenceNoticeDto>> Send([FromBody] SendAbsenceNoticeRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _notices.SendAsync(request, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AbsenceNoticeDto>>> Search([FromQuery] AbsenceNoticeQuery query, CancellationToken ct)
        => Ok(await _notices.SearchAsync(query, ct));

    [HttpPost("{id:int}/read")]
    [Authorize(Roles = AppRoles.DepartmentHead)]
    public async Task<ActionResult<AbsenceNoticeDto>> MarkRead(int id, CancellationToken ct)
        => Ok(await _notices.MarkReadAsync(id, ct));
}
