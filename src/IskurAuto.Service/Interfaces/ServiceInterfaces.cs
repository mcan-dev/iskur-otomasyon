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

public interface IWorkPlanService
{
    Task<WorkPlanCalendarDto> GetMyCalendarAsync(int year, int month, CancellationToken ct = default);
    Task<WorkPlanDto> GetMyPlanAsync(int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<WorkPlanDto>> ListMyPlansAsync(CancellationToken ct = default);
    Task<WorkPlanDto> SaveMyPlanAsync(SaveWorkPlanRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<WorkPlanDto>> SearchAsync(WorkPlanSearchQuery query, CancellationToken ct = default);
    Task<WorkPlanDto> GetByIdAsync(int planId, CancellationToken ct = default);
    Task<WorkPlanCalendarDto> GetStudentCalendarAsync(int studentId, int year, int month, CancellationToken ct = default);
    Task<WorkPlanDto> SaveForStudentAsync(int studentId, SaveWorkPlanRequest request, CancellationToken ct = default);
    Task<WorkPlanDto> ReviewAsync(int planId, ReviewWorkPlanRequest request, CancellationToken ct = default);
}

public interface ICourseDayService
{
    Task<IReadOnlyList<DayOfWeek>> GetMineAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DayOfWeek>> SetMineAsync(CourseDaysRequest request, CancellationToken ct = default);
}

public interface ICalendarService
{
    Task<IReadOnlyList<HolidayDto>> ListHolidaysAsync(int year, CancellationToken ct = default);
    Task<HolidayDto> AddHolidayAsync(HolidayRequest request, CancellationToken ct = default);
    Task DeleteHolidayAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<ExamPeriodDto>> ListExamPeriodsAsync(int year, CancellationToken ct = default);
    Task<ExamPeriodDto> AddExamPeriodAsync(ExamPeriodRequest request, CancellationToken ct = default);
    Task DeleteExamPeriodAsync(int id, CancellationToken ct = default);
}

public interface ILeaveService
{
    Task<LeaveBalanceDto> GetMyBalanceAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveDto>> ListMineAsync(int? year, CancellationToken ct = default);
    Task<LeaveResultDto> RequestAsync(CreateLeaveRequest request, CancellationToken ct = default);
    Task<LeaveDto> CancelAsync(int leaveId, CancellationToken ct = default);
    Task<LeaveDto> AttachDocumentAsync(int leaveId, DocumentUpload file, CancellationToken ct = default);
    Task<DocumentDownload> OpenDocumentAsync(int leaveId, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveDto>> SearchAsync(LeaveSearchQuery query, CancellationToken ct = default);
    Task<LeaveDto> GetByIdAsync(int leaveId, CancellationToken ct = default);
    Task<LeaveResultDto> ReviewAsync(int leaveId, ReviewLeaveRequest request, CancellationToken ct = default);
    Task<LeaveBalanceDto> GetStudentBalanceAsync(int studentId, int year, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveAlertDto>> GetAlertsAsync(int year, CancellationToken ct = default);
}

public interface IAbsenceService
{
    Task<AbsenceDto> RecordAsync(RecordAbsenceRequest request, CancellationToken ct = default);
    Task<AbsenceDto> RevokeAsync(int absenceId, RevokeAbsenceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AbsenceDto>> SearchAsync(AbsenceQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<AbsenceDto>> ListMineAsync(int? year, CancellationToken ct = default);
}

public interface IAttendanceService
{
    // Oturum ve karekod (yetkili kullanıcı)
    Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default);
    Task<SessionDto> CloseSessionAsync(int sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionDto>> ListSessionsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<QrCodeDto> GetCurrentQrAsync(int sessionId, CancellationToken ct = default);

    // Öğrenci
    Task<AttendanceDto> CheckInAsync(CheckInRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceDto>> ListMineAsync(int? year, int? month, CancellationToken ct = default);
    Task<AttendanceComparisonDto> GetMyComparisonAsync(int year, int month, CancellationToken ct = default);

    // Yetkili kullanıcılar
    Task<IReadOnlyList<AttendanceDto>> SearchAsync(AttendanceQuery query, CancellationToken ct = default);
    Task<AttendanceComparisonDto> GetComparisonAsync(int studentId, int year, int month, CancellationToken ct = default);
    Task<AttendanceDto> AddManuallyAsync(ManualAttendanceRequest request, CancellationToken ct = default);
    Task<AttendanceDto> VoidAsync(int attendanceId, VoidAttendanceRequest request, CancellationToken ct = default);
}

public interface ILeavePolicyProvider
{
    Task<LeavePolicy> GetAsync(CancellationToken ct = default);
}

public interface ILeaveSettingsService
{
    Task<LeavePolicyDto> GetAsync(CancellationToken ct = default);
    Task<LeavePolicyDto> UpdateAsync(LeavePolicyDto request, CancellationToken ct = default);
}
