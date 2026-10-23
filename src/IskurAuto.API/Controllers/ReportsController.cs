using IskurAuto.Core.Constants;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Raporlar. Aynı filtreler hem ekrandaki tabloyu hem de Excel/CSV dosyasını üretir.</summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.FacultyAdmin + "," + AppRoles.DepartmentHead)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("{kind}")]
    public async Task<ActionResult<ReportTable>> Get(ReportKind kind, [FromQuery] ReportQuery query, CancellationToken ct)
        => Ok(await _reports.GetAsync(kind, query, ct));

    [HttpGet("{kind}/export")]
    [Produces("text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(ReportKind kind, [FromQuery] ReportQuery query,
        [FromQuery] ReportFormat format, CancellationToken ct)
    {
        var file = await _reports.ExportAsync(kind, query, format == 0 ? ReportFormat.Xlsx : format, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}

/// <summary>İlişik kesme karar kayıtları.</summary>
[ApiController]
[Route("api/terminations")]
[Produces("application/json")]
[Authorize]
public class TerminationsController : ControllerBase
{
    private const string Viewers = AppRoles.FacultyAdmin + "," + AppRoles.DepartmentHead;

    private readonly ITerminationService _terminations;

    public TerminationsController(ITerminationService terminations) => _terminations = terminations;

    /// <summary>Kararı kaydeder ve öğrencinin program katılımını sonlandırır.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TerminationDto>> Record([FromBody] RecordTerminationRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _terminations.RecordAsync(request, ct));

    [HttpGet]
    [Authorize(Roles = Viewers)]
    public async Task<ActionResult<IReadOnlyList<TerminationDto>>> Search([FromQuery] TerminationQuery query, CancellationToken ct)
        => Ok(await _terminations.SearchAsync(query, ct));
}
