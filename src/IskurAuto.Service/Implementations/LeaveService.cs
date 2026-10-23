using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Leaves;
using IskurAuto.Service.Mapping;

namespace IskurAuto.Service.Implementations;

public class LeaveService : ILeaveService
{
    private const long MaxDocumentBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    private readonly ILeaveRepository _leaves;
    private readonly IAbsenceRepository _absences;
    private readonly IStudentRepository _students;
    private readonly IHolidayRepository _holidays;
    private readonly IWorkPlanRepository _plans;
    private readonly IDocumentStore _documents;
    private readonly ILeavePolicyProvider _policies;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;
    private readonly WorkPlanSettings _workSettings;

    public LeaveService(
        ILeaveRepository leaves, IAbsenceRepository absences, IStudentRepository students,
        IHolidayRepository holidays, IWorkPlanRepository plans, IDocumentStore documents,
        ILeavePolicyProvider policies, IUnitOfWork uow, IClock clock, IAuditService audit,
        ICurrentUser current, WorkPlanSettings workSettings)
    {
        _leaves = leaves;
        _absences = absences;
        _students = students;
        _holidays = holidays;
        _plans = plans;
        _documents = documents;
        _policies = policies;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
        _workSettings = workSettings;
    }

    public async Task<LeaveBalanceDto> GetMyBalanceAsync(int year, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        return await BalanceAsync(student.Id, year, ct);
    }

    public async Task<IReadOnlyList<LeaveDto>> ListMineAsync(int? year, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria { StudentId = student.Id, Year = year }, ct);
        return leaves.Select(l => l.ToDto()).ToList();
    }

    public async Task<LeaveResultDto> RequestAsync(CreateLeaveRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        if (!student.IsActive)
            throw new ConflictException("Programda aktif olmadığınız için izin talep edemezsiniz.");

        ValidateRequest(request);

        if (await _leaves.HasOverlapAsync(student.Id, request.StartDate, request.EndDate, ct: ct))
            throw new ConflictException("Bu tarihlerle çakışan bir izin talebiniz zaten var.");

        var days = await CountDaysAsync(student.Id, request, ct);
        if (days <= 0)
            throw new DomainValidationException(nameof(request.StartDate), "Seçilen aralıkta izin kullanılacak bir çalışma günü yok.");

        var leave = new LeaveRequest
        {
            StudentProfileId = student.Id,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Hours = request.Hours,
            DaysCounted = days,
            Reason = request.Reason.Trim(),
            Description = request.Description?.Trim(),
            AppliedOn = _clock.Today
        };
        await _leaves.AddAsync(leave);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.LeaveRequested, nameof(LeaveRequest), leave.Id,
            $"{request.StartDate:yyyy-MM-dd} / {request.EndDate:yyyy-MM-dd}, {days:0.##} gün", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ResultAsync(leave.Id, ct);
    }

    public async Task<LeaveDto> CancelAsync(int leaveId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var leave = await LoadOwnedAsync(leaveId, ct);
        if (leave.Status != LeaveStatus.Pending)
            throw new ConflictException("Yalnızca bekleyen talepler iptal edilebilir.");

        leave.Status = LeaveStatus.Cancelled;
        await _audit.RecordAsync(AuditActions.LeaveCancelled, nameof(LeaveRequest), leave.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return leave.ToDto();
    }

    public async Task<LeaveDto> AttachDocumentAsync(int leaveId, DocumentUpload file, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var leave = await LoadOwnedAsync(leaveId, ct);
        if (leave.Status is LeaveStatus.Rejected or LeaveStatus.Cancelled)
            throw new ConflictException("Reddedilen veya iptal edilen talebe belge eklenemez.");

        var extension = Path.GetExtension(file.FileName);
        if (!DocumentTypes.TryGetValue(extension, out var contentType))
            throw new DomainValidationException("File", "Yalnızca PDF, JPG veya PNG yüklenebilir.");
        if (file.Length <= 0 || file.Length > MaxDocumentBytes)
            throw new DomainValidationException("File", "Dosya boş olamaz ve 5 MB'ı aşamaz.");

        leave.DocumentStoredName = await _documents.SaveAsync(file.Content, extension.ToLowerInvariant(), ct);
        leave.DocumentFileName = Path.GetFileName(file.FileName);
        leave.DocumentContentType = contentType;

        await _audit.RecordAsync(AuditActions.LeaveDocumentAdded, nameof(LeaveRequest), leave.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return leave.ToDto();
    }

    public async Task<DocumentDownload> OpenDocumentAsync(int leaveId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor, UserRole.Student);

        var leave = await LoadVisibleAsync(leaveId, ct);
        if (!leave.HasDocument)
            throw new NotFoundException("Bu talepte belge yok.");

        var content = await _documents.OpenAsync(leave.DocumentStoredName!, ct)
            ?? throw new NotFoundException("Belge dosyası bulunamadı.");

        if (_current.Role != UserRole.Student)
        {
            await _audit.RecordAsync(AuditActions.LeaveDocumentViewed, nameof(LeaveRequest), leave.Id, ct: ct);
            await _uow.SaveChangesAsync(ct);
        }

        return new DocumentDownload
        {
            Content = content,
            FileName = leave.DocumentFileName ?? "belge",
            ContentType = leave.DocumentContentType ?? "application/octet-stream"
        };
    }

    public async Task<IReadOnlyList<LeaveDto>> SearchAsync(LeaveSearchQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new LeaveSearchCriteria
        {
            Year = query.Year,
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            Status = query.Status
        };
        ApplyScope(criteria);

        var leaves = await _leaves.SearchAsync(criteria, ct);
        return leaves.Select(l => l.ToDto()).ToList();
    }

    public async Task<LeaveDto> GetByIdAsync(int leaveId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor, UserRole.Student);

        return (await LoadVisibleAsync(leaveId, ct)).ToDto();
    }

    public async Task<LeaveResultDto> ReviewAsync(int leaveId, ReviewLeaveRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var leave = await _leaves.GetDetailAsync(leaveId, ct)
            ?? throw new NotFoundException("İzin talebi bulunamadı.");

        if (leave.Status != LeaveStatus.Pending)
            throw new ConflictException("Bu talep zaten sonuçlandırılmış.");

        var note = request.Note?.Trim();
        if (!request.Approve && string.IsNullOrEmpty(note))
            throw new DomainValidationException(nameof(request.Note), "Reddederken gerekçe yazmalısınız.");

        leave.Status = request.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ReviewedByUserId = _current.UserId;
        leave.ReviewedAt = _clock.UtcNow;
        leave.ReviewNote = note;

        await _audit.RecordAsync(request.Approve ? AuditActions.LeaveApproved : AuditActions.LeaveRejected,
            nameof(LeaveRequest), leave.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return await ResultAsync(leave.Id, ct);
    }

    public async Task<LeaveBalanceDto> GetStudentBalanceAsync(int studentId, int year, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var student = await _students.GetDetailAsync(studentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        _current.EnsureCanView(student);

        return await BalanceAsync(student.Id, year, ct);
    }

    public async Task<IReadOnlyList<LeaveAlertDto>> GetAlertsAsync(int year, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var leaveCriteria = new LeaveSearchCriteria { Year = year };
        ApplyScope(leaveCriteria);
        var absenceCriteria = new AbsenceSearchCriteria
        {
            Year = year,
            DepartmentId = leaveCriteria.DepartmentId,
            AdvisorUserId = leaveCriteria.AdvisorUserId
        };

        var leaves = await _leaves.SearchAsync(leaveCriteria, ct);
        var absences = await _absences.SearchAsync(absenceCriteria, ct);
        var policy = await _policies.GetAsync(ct);

        var students = leaves.Select(l => l.StudentProfile)
            .Concat(absences.Select(a => a.StudentProfile))
            .DistinctBy(s => s.Id);

        return students
            .Select(s => new
            {
                Student = s,
                Balance = LeaveBalanceCalculator.Calculate(policy,
                    leaves.Where(l => l.StudentProfileId == s.Id),
                    absences.Where(a => a.StudentProfileId == s.Id), s.LeaveDayAllowance)
            })
            .Where(x => x.Balance.Warnings.Count > 0)
            .OrderByDescending(x => x.Balance.Warnings.Max())
            .ThenBy(x => x.Student.User.FullName)
            .Select(x => new LeaveAlertDto
            {
                StudentId = x.Student.Id,
                StudentName = x.Student.User.FullName,
                DepartmentName = x.Student.Department.Name,
                Balance = x.Balance.ToDto(year)
            })
            .ToList();
    }

    private void ValidateRequest(CreateLeaveRequest request)
    {
        var errors = new ValidationErrors();

        if (request.EndDate < request.StartDate)
            errors.Add(nameof(request.EndDate), "Bitiş tarihi başlangıçtan önce olamaz.");
        if (request.StartDate.Year != request.EndDate.Year)
            errors.Add(nameof(request.EndDate), "İzin iki farklı yıla yayılamaz; yıl sonunda ikiye bölerek talep edin.");
        if (!Enum.IsDefined(request.Type))
            errors.Add(nameof(request.Type), "Geçersiz izin türü.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            errors.Add(nameof(request.Reason), "İzin gerekçesi yazmalısınız.");

        if (request.Hours is not null)
        {
            if (request.StartDate != request.EndDate)
                errors.Add(nameof(request.Hours), "Saatlik izin yalnızca tek gün için alınabilir.");
            if (request.Hours >= _workSettings.DailyHours)
                errors.Add(nameof(request.Hours), "Saatlik izin tam gün süresinden kısa olmalıdır; tam gün için saat girmeyin.");
        }

        errors.ThrowIfAny();
    }

    private async Task<decimal> CountDaysAsync(int studentId, CreateLeaveRequest request, CancellationToken ct)
    {
        var holidays = (await _holidays.GetBetweenAsync(request.StartDate, request.EndDate, ct))
            .Select(h => h.Date).ToHashSet();
        var plans = await _plans.GetPlansInRangeAsync(studentId, request.StartDate, request.EndDate, ct);

        return LeaveDayCounter.Count(request.StartDate, request.EndDate, request.Hours, holidays, plans, _workSettings);
    }

    private async Task<LeaveBalanceDto> BalanceAsync(int studentId, int year, CancellationToken ct)
    {
        var policy = await _policies.GetAsync(ct);
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria { StudentId = studentId, Year = year }, ct);
        var absences = await _absences.SearchAsync(new AbsenceSearchCriteria { StudentId = studentId, Year = year }, ct);

        var student = await _students.GetByIdAsync(studentId);

        return LeaveBalanceCalculator.Calculate(policy, leaves, absences, student?.LeaveDayAllowance).ToDto(year);
    }

    private async Task<LeaveResultDto> ResultAsync(int leaveId, CancellationToken ct)
    {
        var leave = (await _leaves.GetDetailAsync(leaveId, ct))!;
        return new LeaveResultDto
        {
            Leave = leave.ToDto(),
            Balance = await BalanceAsync(leave.StudentProfileId, leave.StartDate.Year, ct)
        };
    }

    private void ApplyScope(LeaveSearchCriteria criteria)
    {
        switch (_current.Role)
        {
            case UserRole.DepartmentHead:
                criteria.DepartmentId = _current.DepartmentId
                    ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış.");
                break;
            case UserRole.Advisor:
                criteria.AdvisorUserId = _current.UserId;
                break;
        }
    }

    private async Task<StudentProfile> LoadMineAsync(CancellationToken ct)
        => await _students.GetDetailByUserIdAsync(_current.UserId, ct)
           ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

    private async Task<LeaveRequest> LoadOwnedAsync(int leaveId, CancellationToken ct)
    {
        var student = await LoadMineAsync(ct);
        var leave = await _leaves.GetDetailAsync(leaveId, ct);

        if (leave is null || leave.StudentProfileId != student.Id)
            throw new NotFoundException("İzin talebi bulunamadı.");

        return leave;
    }

    private async Task<LeaveRequest> LoadVisibleAsync(int leaveId, CancellationToken ct)
    {
        var leave = await _leaves.GetDetailAsync(leaveId, ct)
            ?? throw new NotFoundException("İzin talebi bulunamadı.");

        if (!_current.CanView(leave.StudentProfile))
            throw new NotFoundException("İzin talebi bulunamadı.");

        return leave;
    }
}
