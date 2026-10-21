using IskurAuto.Core.Constants;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Aylık çalışma planları.</summary>
[ApiController]
[Route("api/work-plans")]
[Produces("application/json")]
[Authorize]
public class WorkPlansController : ControllerBase
{
    private readonly IWorkPlanService _plans;

    public WorkPlansController(IWorkPlanService plans) => _plans = plans;

    /// <summary>Öğrencinin kendi takvimi: her gün için renk, etiket ve seçim durumu.</summary>
    [HttpGet("me/calendar")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<WorkPlanCalendarDto>> MyCalendar([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await _plans.GetMyCalendarAsync(year, month, ct));

    /// <summary>Öğrencinin belirli bir aya ait planı.</summary>
    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<WorkPlanDto>> MyPlan([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await _plans.GetMyPlanAsync(year, month, ct));

    /// <summary>Öğrencinin geçmiş dahil tüm planları.</summary>
    [HttpGet("me/history")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyList<WorkPlanDto>>> MyHistory(CancellationToken ct)
        => Ok(await _plans.ListMyPlansAsync(ct));

    /// <summary>Planı kaydeder; aynı ay için tekrar çağrılırsa planı günceller.</summary>
    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkPlanDto>> SaveMine([FromBody] SaveWorkPlanRequest request, CancellationToken ct)
        => Ok(await _plans.SaveMyPlanAsync(request, ct));

    /// <summary>Personel için ay bazında plan listesi. Sonuç rolün kapsamıyla sınırlanır.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<IReadOnlyList<WorkPlanDto>>> Search([FromQuery] WorkPlanSearchQuery query, CancellationToken ct)
        => Ok(await _plans.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkPlanDto>> GetById(int id, CancellationToken ct)
        => Ok(await _plans.GetByIdAsync(id, ct));

    /// <summary>Belirli bir öğrencinin takvimi (personel).</summary>
    [HttpGet("students/{studentId:int}/calendar")]
    [Authorize(Roles = AppRoles.AllStaff)]
    public async Task<ActionResult<WorkPlanCalendarDto>> StudentCalendar(int studentId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await _plans.GetStudentCalendarAsync(studentId, year, month, ct));

    /// <summary>Fakülte yönetimi öğrenci adına plan girer veya düzeltir.</summary>
    [HttpPut("students/{studentId:int}")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<WorkPlanDto>> SaveForStudent(int studentId, [FromBody] SaveWorkPlanRequest request, CancellationToken ct)
        => Ok(await _plans.SaveForStudentAsync(studentId, request, ct));

    /// <summary>Planı onaylar veya gerekçeyle reddeder.</summary>
    [HttpPost("{id:int}/review")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<WorkPlanDto>> Review(int id, [FromBody] ReviewWorkPlanRequest request, CancellationToken ct)
        => Ok(await _plans.ReviewAsync(id, request, ct));
}

/// <summary>Öğrencinin beyan ettiği ders günleri.</summary>
[ApiController]
[Route("api/students/me/course-days")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Student)]
public class CourseDaysController : ControllerBase
{
    private readonly ICourseDayService _courseDays;

    public CourseDaysController(ICourseDayService courseDays) => _courseDays = courseDays;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DayOfWeek>>> Get(CancellationToken ct)
        => Ok(await _courseDays.GetMineAsync(ct));

    [HttpPut]
    public async Task<ActionResult<IReadOnlyList<DayOfWeek>>> Set([FromBody] CourseDaysRequest request, CancellationToken ct)
        => Ok(await _courseDays.SetMineAsync(request, ct));
}

/// <summary>Resmî tatiller ve sınav dönemleri.</summary>
[ApiController]
[Route("api/calendar")]
[Produces("application/json")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendar;

    public CalendarController(ICalendarService calendar) => _calendar = calendar;

    [HttpGet("holidays")]
    public async Task<ActionResult<IReadOnlyList<HolidayDto>>> Holidays([FromQuery] int year, CancellationToken ct)
        => Ok(await _calendar.ListHolidaysAsync(year, ct));

    [HttpPost("holidays")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<HolidayDto>> AddHoliday([FromBody] HolidayRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _calendar.AddHolidayAsync(request, ct));

    [HttpDelete("holidays/{id:int}")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<IActionResult> DeleteHoliday(int id, CancellationToken ct)
    {
        await _calendar.DeleteHolidayAsync(id, ct);
        return NoContent();
    }

    [HttpGet("exam-periods")]
    public async Task<ActionResult<IReadOnlyList<ExamPeriodDto>>> ExamPeriods([FromQuery] int year, CancellationToken ct)
        => Ok(await _calendar.ListExamPeriodsAsync(year, ct));

    [HttpPost("exam-periods")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<ActionResult<ExamPeriodDto>> AddExamPeriod([FromBody] ExamPeriodRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _calendar.AddExamPeriodAsync(request, ct));

    [HttpDelete("exam-periods/{id:int}")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    public async Task<IActionResult> DeleteExamPeriod(int id, CancellationToken ct)
    {
        await _calendar.DeleteExamPeriodAsync(id, ct);
        return NoContent();
    }
}
