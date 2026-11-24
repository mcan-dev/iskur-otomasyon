using IskurAuto.Core.Constants;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Kimin hangi gün çalıştığı ve geçmiş yıllarda çalışanlar.</summary>
[ApiController]
[Route("api/work-roster")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.AllStaff)]
public class WorkRosterController : ControllerBase
{
    private readonly IWorkRosterService _roster;

    public WorkRosterController(IWorkRosterService roster) => _roster = roster;

    /// <summary>Seçilen ayın her günü için o gün çalışan öğrenciler (sorumlu personel, birim yetkilisi, sekreterlik).</summary>
    [HttpGet]
    public async Task<ActionResult<WorkRosterDto>> Month([FromQuery] RosterQuery query, CancellationToken ct)
        => Ok(await _roster.GetMonthAsync(query, ct));

    /// <summary>Seçilen yılda çalışan öğrenciler (birim yetkilisi ve sekreterlik).</summary>
    [HttpGet("year")]
    [Authorize(Roles = AppRoles.FacultyAdmin + "," + AppRoles.DepartmentHead)]
    public async Task<ActionResult<YearWorkersDto>> Year([FromQuery] YearWorkersQuery query, CancellationToken ct)
        => Ok(await _roster.GetYearAsync(query, ct));
}
