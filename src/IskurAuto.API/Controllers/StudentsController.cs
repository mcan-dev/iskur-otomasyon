using IskurAuto.Core.Constants;
using IskurAuto.Core.Models;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>
/// Öğrenci kayıtları. Hangi rolün hangi öğrenciyi görebildiği servis katmanında zorlanır:
/// Fakülte yönetimi hepsini, Bölüm Başkanı kendi bölümünü, Danışman kendi öğrencilerini,
/// Öğrenci yalnızca kendisini görür.
/// </summary>
[ApiController]
[Route("api/students")]
[Produces("application/json")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _students;

    public StudentsController(IStudentService students) => _students = students;

    // ─── Öğrenci: kendi profili ───────────────────────────────────────────────

    /// <summary>Öğrencinin kendi profili (TC ve IBAN ham görünür).</summary>
    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDetailDto>> GetMine(CancellationToken ct)
        => Ok(await _students.GetMyProfileAsync(ct));

    /// <summary>Öğrenci kendi telefon ve IBAN bilgisini günceller.</summary>
    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudentDetailDto>> UpdateMine([FromBody] UpdateMyProfileRequest request, CancellationToken ct)
        => Ok(await _students.UpdateMyProfileAsync(request, ct));

    // ─── Personel: listeleme ve görüntüleme ───────────────────────────────────

    /// <summary>
    /// Öğrenci arama/filtreleme (belge madde 10). Sonuç rolün kapsamıyla otomatik sınırlanır.
    /// Listede T.C. kimlik no maskelidir.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AllStaff)]
    [ProducesResponseType(typeof(PagedResult<StudentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentSummaryDto>>> Search([FromQuery] StudentSearchQuery query, CancellationToken ct)
        => Ok(await _students.SearchAsync(query, ct));

    /// <summary>
    /// Öğrenci detayı, tüm geçmişiyle. Ham TC/IBAN yalnızca fakülte yönetimine ve öğrencinin kendisine gösterilir;
    /// yönetimin ham veriye erişimi denetim kaydına yazılır.
    /// </summary>
    /// <response code="404">Öğrenci yok ya da bu rolün kapsamı dışında.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDetailDto>> GetById(int id, CancellationToken ct)
        => Ok(await _students.GetByIdAsync(id, ct));

    // ─── Yönetim işlemleri ────────────────────────────────────────────────────

    /// <summary>Öğrencinin program katılımını sonlandırır (kayıt silinmez, pasif olur).</summary>
    [HttpPost("{id:int}/end-participation")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDetailDto>> EndParticipation(int id, [FromBody] EndParticipationRequest request, CancellationToken ct)
        => Ok(await _students.EndParticipationAsync(id, request, ct));

    /// <summary>Pasif öğrenciyi yeni bir katılım dönemiyle programa yeniden dahil eder.</summary>
    [HttpPost("{id:int}/start-participation")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDetailDto>> StartParticipation(int id, [FromBody] StartParticipationRequest request, CancellationToken ct)
        => Ok(await _students.StartParticipationAsync(id, request, ct));

    /// <summary>Öğrenciye özel yıllık izin hakkını (gün) belirler; boş gönderilirse genel hak geçerli olur.</summary>
    [HttpPut("{id:int}/leave-allowance")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDetailDto>> SetLeaveAllowance(int id, [FromBody] SetLeaveAllowanceRequest request, CancellationToken ct)
        => Ok(await _students.SetLeaveAllowanceAsync(id, request, ct));

    /// <summary>Öğrenciyi bir çalışma birimine atar; önceki atama geçmişe taşınır.</summary>
    [HttpPut("{id:int}/unit")]
    [Authorize(Roles = AppRoles.FacultyAdmin)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDetailDto>> AssignUnit(int id, [FromBody] AssignUnitRequest request, CancellationToken ct)
        => Ok(await _students.AssignUnitAsync(id, request, ct));

    /// <summary>Öğrenciye danışman atar; önceki danışman geçmişe taşınır. Bölüm başkanı yalnızca kendi bölümü için.</summary>
    [HttpPut("{id:int}/advisor")]
    [Authorize(Roles = AppRoles.FacultyAdmin + "," + AppRoles.DepartmentHead)]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDetailDto>> AssignAdvisor(int id, [FromBody] AssignAdvisorRequest request, CancellationToken ct)
        => Ok(await _students.AssignAdvisorAsync(id, request, ct));
}
