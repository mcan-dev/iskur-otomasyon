using IskurAuto.Core.Models;
using IskurAuto.Service.DTOs;

namespace IskurAuto.Service.Interfaces;

// Controller'lar bu arayüzlere bağımlıdır, somut sınıflara değil (Dependency Inversion):
// böylece servis uygulaması değişse ya da testte taklit edilse bile controller kodu değişmez.

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<RegistrationResultDto> RegisterStudentAsync(StudentRegistrationRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default);
}

public interface IStudentService
{
    // Öğrencinin kendisi
    Task<StudentDetailDto> GetMyProfileAsync(CancellationToken ct = default);
    Task<StudentDetailDto> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken ct = default);

    // Personel (rol kapsamına göre filtrelenmiş)
    Task<PagedResult<StudentSummaryDto>> SearchAsync(StudentSearchQuery query, CancellationToken ct = default);
    Task<StudentDetailDto> GetByIdAsync(int studentId, CancellationToken ct = default);

    // Yönetim işlemleri
    Task<StudentDetailDto> EndParticipationAsync(int studentId, EndParticipationRequest request, CancellationToken ct = default);
    Task<StudentDetailDto> StartParticipationAsync(int studentId, StartParticipationRequest request, CancellationToken ct = default);
    Task<StudentDetailDto> AssignUnitAsync(int studentId, AssignUnitRequest request, CancellationToken ct = default);
    Task<StudentDetailDto> AssignAdvisorAsync(int studentId, AssignAdvisorRequest request, CancellationToken ct = default);
}

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> SearchAsync(UserSearchQuery query, CancellationToken ct = default);
    Task<UserDto> CreateStaffAsync(CreateStaffUserRequest request, CancellationToken ct = default);
    Task<UserDto> SetActiveAsync(int userId, bool isActive, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<DepartmentDto> CreateAsync(DepartmentUpsertRequest request, CancellationToken ct = default);
    Task<DepartmentDto> UpdateAsync(int id, DepartmentUpsertRequest request, CancellationToken ct = default);
}

public interface IWorkUnitService
{
    Task<IReadOnlyList<WorkUnitDto>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<WorkUnitDto> CreateAsync(WorkUnitUpsertRequest request, CancellationToken ct = default);
    Task<WorkUnitDto> UpdateAsync(int id, WorkUnitUpsertRequest request, CancellationToken ct = default);
}

public interface IAuditService
{
    /// <summary>
    /// Denetim kaydı ekler ama KAYDETMEZ: çağıran servis, iş değişikliğiyle aynı SaveChanges içinde
    /// kaydeder. Böylece "işlem oldu ama kaydı yok" ya da tersi durum oluşamaz.
    /// </summary>
    Task RecordAsync(string action, string entityName, int? entityId = null, string? details = null,
        int? actorUserId = null, string? actorEmail = null, CancellationToken ct = default);

    Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default);
}
