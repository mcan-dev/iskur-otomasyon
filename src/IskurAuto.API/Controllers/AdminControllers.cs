using IskurAuto.Core.Constants;
using IskurAuto.Core.Models;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Personel hesapları.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.FacultyAdmin + "," + AppRoles.DepartmentHead)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Kullanıcı listesi. Bölüm başkanı yalnızca kendi bölümündekileri görür (danışman seçimi için).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Search([FromQuery] UserSearchQuery query, CancellationToken ct)
        => Ok(await _users.SearchAsync(query, ct));

    /// <summary>Personel hesabı oluşturur (Fakülte yönetimi, Bölüm Başkanı veya Danışman).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> CreateStaff([FromBody] CreateStaffUserRequest request, CancellationToken ct)
    {
        var created = await _users.CreateStaffAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Personel hesabını aktif/pasif yapar (hesap silinmez).</summary>
    [HttpPut("{id:int}/active")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> SetActive(int id, [FromBody] SetUserActiveRequest request, CancellationToken ct)
        => Ok(await _users.SetActiveAsync(id, request.IsActive, ct));
}

/// <summary>Bölüm tanımları.</summary>
[ApiController]
[Route("api/departments")]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments) => _departments = departments;

    /// <summary>
    /// Bölüm listesi. Giriş gerektirmez: öğrenci kayıt formundaki bölüm seçimi için gereklidir.
    /// Pasif bölümler (includeInactive=true) yalnızca fakülte yönetimine görünür.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> List([FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _departments.ListAsync(includeInactive, ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] DepartmentUpsertRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _departments.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartmentDto>> Update(int id, [FromBody] DepartmentUpsertRequest request, CancellationToken ct)
        => Ok(await _departments.UpdateAsync(id, request, ct));
}

/// <summary>Öğrencilerin çalışabileceği birimler.</summary>
[ApiController]
[Route("api/work-units")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.FacultyAdmin)]
public class WorkUnitsController : ControllerBase
{
    private readonly IWorkUnitService _units;

    public WorkUnitsController(IWorkUnitService units) => _units = units;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkUnitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkUnitDto>>> List([FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _units.ListAsync(includeInactive, ct));

    [HttpPost]
    [ProducesResponseType(typeof(WorkUnitDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkUnitDto>> Create([FromBody] WorkUnitUpsertRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _units.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(WorkUnitDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkUnitDto>> Update(int id, [FromBody] WorkUnitUpsertRequest request, CancellationToken ct)
        => Ok(await _units.UpdateAsync(id, request, ct));
}

/// <summary>Denetim kayıtları: kim, ne zaman, neyi yaptı (belge madde 9 ve 11).</summary>
[ApiController]
[Route("api/audit-logs")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.FacultyAdmin)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditLogsController(IAuditService audit) => _audit = audit;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Search([FromQuery] AuditLogQuery query, CancellationToken ct)
        => Ok(await _audit.SearchAsync(query, ct));
}
