using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Attendance;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;

namespace IskurAuto.Service.Implementations;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRecordRepository _records;
    private readonly IStudentRepository _students;
    private readonly IWorkPlanRepository _plans;
    private readonly ILeaveRepository _leaves;
    private readonly IAbsenceRepository _absences;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AttendanceService(IAttendanceSessionRepository sessions, IAttendanceRecordRepository records,
        IStudentRepository students, IWorkPlanRepository plans, ILeaveRepository leaves, IAbsenceRepository absences,
        IUnitOfWork uow, IClock clock, IAuditService audit, ICurrentUser current)
    {
        _sessions = sessions;
        _records = records;
        _students = students;
        _plans = plans;
        _leaves = leaves;
        _absences = absences;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    // ── Oturum ve karekod ────────────────────────────────────────────────────

    public async Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var departmentId = _current.Role == UserRole.DepartmentHead
            ? _current.DepartmentId ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış.")
            : request.DepartmentId;

        ValidateSessionTimes(request);

        if (await _sessions.HasOverlapAsync(request.Date, request.StartTime, request.EndTime, departmentId, ct))
            throw new ConflictException("Bu tarih ve saat aralığında açık bir oturum zaten var.");

        var session = new AttendanceSession
        {
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Title = request.Title?.Trim(),
            DepartmentId = departmentId,
            SecretKey = QrToken.NewSecret(),
            CreatedByUserId = _current.UserId
        };
        await _sessions.AddAsync(session);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.SessionCreated, nameof(AttendanceSession), session.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await LoadSessionInScopeAsync(session.Id, ct)).ToDto(_clock.LocalNow);
    }

    public async Task<SessionDto> CloseSessionAsync(int sessionId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var session = await LoadSessionInScopeAsync(sessionId, ct);
        if (session.IsClosed)
            throw new ConflictException("Oturum zaten kapatılmış.");

        session.ClosedAt = _clock.UtcNow;
        await _audit.RecordAsync(AuditActions.SessionClosed, nameof(AttendanceSession), session.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return session.ToDto(_clock.LocalNow);
    }

    public async Task<IReadOnlyList<SessionDto>> ListSessionsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var today = _clock.Today;
        var departmentId = _current.Role == UserRole.DepartmentHead ? _current.DepartmentId : null;
        var sessions = await _sessions.ListAsync(from ?? today, to ?? from ?? today, departmentId, ct);

        var localNow = _clock.LocalNow;
        return sessions.Select(s => s.ToDto(localNow)).ToList();
    }

    public async Task<QrCodeDto> GetCurrentQrAsync(int sessionId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var session = await LoadSessionInScopeAsync(sessionId, ct);
        if (!session.IsActiveAt(_clock.LocalNow))
            throw new DomainValidationException("Oturum şu an aktif değil; karekod yalnızca belirlenen gün ve saat aralığında üretilir.");

        var now = _clock.UtcNow;
        return new QrCodeDto
        {
            SessionId = session.Id,
            Token = QrToken.Create(session.Id, session.SecretKey, now),
            RefreshAtUtc = QrToken.RefreshAt(now),
            WindowSeconds = QrToken.WindowSeconds
        };
    }

    // ── Öğrenci ──────────────────────────────────────────────────────────────

    public async Task<AttendanceDto> CheckInAsync(CheckInRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await _students.GetDetailByUserIdAsync(_current.UserId, ct)
            ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

        if (!QrToken.TryReadSessionId(request.Token, out var sessionId))
            throw await RejectAsync("Karekod okunamadı.", null, ct);

        var session = await _sessions.GetDetailAsync(sessionId, ct);
        if (session is null || !QrToken.IsValid(request.Token, session.Id, session.SecretKey, _clock.UtcNow))
            throw await RejectAsync("Karekodun süresi dolmuş veya geçersiz. Ekrandaki güncel karekodu okutun.", session?.Id, ct);

        var localNow = _clock.LocalNow;
        if (session.IsClosed || !session.IsActiveAt(localNow))
            throw await RejectAsync("Bu oturum şu an devam kaydına açık değil.", session.Id, ct);

        if (!student.IsActive)
            throw await RejectAsync("Programdaki katılımınız sona erdiği için devam kaydı oluşturulamaz.", session.Id, ct);

        if (session.DepartmentId is int departmentId && departmentId != student.DepartmentId)
            throw await RejectAsync("Bu oturum bölümünüz için açılmamış.", session.Id, ct);

        if (await _records.ActiveExistsAsync(student.Id, session.Id, ct))
            throw new ConflictException("Bu oturum için devam kaydınız zaten var.");

        var planDay = await FindApprovedPlanDayAsync(student.Id, session.Date, ct);
        if (planDay is null)
            throw await RejectAsync("Bu gün için onaylanmış bir çalışma planınız bulunmuyor.", session.Id, ct);

        if (await HasApprovedFullDayLeaveAsync(student.Id, session.Date, ct))
            throw await RejectAsync("Bu gün için onaylı izniniz bulunuyor.", session.Id, ct);

        var record = new AttendanceRecord
        {
            StudentProfileId = student.Id,
            AttendanceSessionId = session.Id,
            Date = session.Date,
            CheckedInAt = _clock.UtcNow,
            WorkPlanDayId = planDay.Id
        };
        await _records.AddAsync(record);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.CheckInSucceeded, nameof(AttendanceRecord), record.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await _records.GetDetailAsync(record.Id, ct))!.ToDto();
    }

    public async Task<IReadOnlyList<AttendanceDto>> ListMineAsync(int? year, int? month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await _students.GetDetailByUserIdAsync(_current.UserId, ct)
            ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

        var records = await _records.SearchAsync(new AttendanceSearchCriteria
        {
            StudentId = student.Id,
            Year = year,
            Month = year is null ? null : month
        }, ct);
        return records.Select(r => r.ToDto()).ToList();
    }

    public async Task<AttendanceComparisonDto> GetMyComparisonAsync(int year, int month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await _students.GetDetailByUserIdAsync(_current.UserId, ct)
            ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

        return await CompareAsync(student, year, month, ct);
    }

    // ── Yetkili kullanıcılar ─────────────────────────────────────────────────

    public async Task<IReadOnlyList<AttendanceDto>> SearchAsync(AttendanceQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new AttendanceSearchCriteria
        {
            Date = query.Date,
            Year = query.Year,
            Month = query.Month,
            SessionId = query.SessionId,
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            IncludeVoided = query.IncludeVoided
        };

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

        var records = await _records.SearchAsync(criteria, ct);
        return records.Select(r => r.ToDto()).ToList();
    }

    public async Task<AttendanceComparisonDto> GetComparisonAsync(int studentId, int year, int month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var student = await _students.GetDetailAsync(studentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        _current.EnsureCanView(student);

        return await CompareAsync(student, year, month, ct);
    }

    public async Task<AttendanceDto> AddManuallyAsync(ManualAttendanceRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var reason = RequireReason(request.Reason, nameof(request.Reason), "Düzeltme gerekçesini yazmalısınız.");

        var student = await _students.GetDetailAsync(request.StudentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        var session = await _sessions.GetDetailAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Oturum bulunamadı.");

        if (session.DepartmentId is int departmentId && departmentId != student.DepartmentId)
            throw new DomainValidationException(nameof(request.StudentId), "Öğrenci bu oturumun kapsamında değil.");
        if (await _records.ActiveExistsAsync(student.Id, session.Id, ct))
            throw new ConflictException("Bu öğrencinin bu oturum için zaten geçerli bir devam kaydı var.");

        var planDay = await FindApprovedPlanDayAsync(student.Id, session.Date, ct);
        var record = new AttendanceRecord
        {
            StudentProfileId = student.Id,
            AttendanceSessionId = session.Id,
            Date = session.Date,
            CheckedInAt = _clock.UtcNow,
            WorkPlanDayId = planDay?.Id,
            IsManual = true,
            ManualReason = reason,
            CreatedByUserId = _current.UserId
        };
        await _records.AddAsync(record);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.AttendanceAddedManually, nameof(AttendanceRecord), record.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await _records.GetDetailAsync(record.Id, ct))!.ToDto();
    }

    public async Task<AttendanceDto> VoidAsync(int attendanceId, VoidAttendanceRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var reason = RequireReason(request.Reason, nameof(request.Reason), "İptal gerekçesini yazmalısınız.");

        var record = await _records.GetDetailAsync(attendanceId, ct)
            ?? throw new NotFoundException("Devam kaydı bulunamadı.");
        if (record.IsVoided)
            throw new ConflictException("Bu kayıt zaten iptal edilmiş.");

        record.VoidedAt = _clock.UtcNow;
        record.VoidedByUserId = _current.UserId;
        record.VoidReason = reason;

        await _audit.RecordAsync(AuditActions.AttendanceVoided, nameof(AttendanceRecord), record.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return (await _records.GetDetailAsync(record.Id, ct))!.ToDto();
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private void ValidateSessionTimes(CreateSessionRequest request)
    {
        var errors = new ValidationErrors();

        if (request.EndTime <= request.StartTime)
            errors.Add(nameof(request.EndTime), "Bitiş saati başlangıç saatinden sonra olmalıdır.");

        var localNow = _clock.LocalNow;
        if (request.Date < _clock.Today)
            errors.Add(nameof(request.Date), "Geçmiş bir gün için oturum açılamaz.");
        else if (request.Date == _clock.Today && request.EndTime <= TimeOnly.FromDateTime(localNow))
            errors.Add(nameof(request.EndTime), "Bitiş saati geçmiş; bugün için ileri bir saat seçin.");

        errors.ThrowIfAny();
    }

    /// <summary>Bölüm başkanı yalnızca kendi bölümünün (ve fakülte geneli) oturumlarını görür.</summary>
    private async Task<AttendanceSession> LoadSessionInScopeAsync(int sessionId, CancellationToken ct)
    {
        var session = await _sessions.GetDetailAsync(sessionId, ct)
            ?? throw new NotFoundException("Oturum bulunamadı.");

        var outOfScope = _current.Role == UserRole.DepartmentHead
            && session.DepartmentId != _current.DepartmentId;
        if (outOfScope)
            throw new NotFoundException("Oturum bulunamadı.");

        return session;
    }

    private async Task<WorkPlanDay?> FindApprovedPlanDayAsync(int studentId, DateOnly date, CancellationToken ct)
    {
        var plan = await _plans.GetWithDaysAsync(studentId, date.Year, date.Month, ct);
        return plan is { Status: WorkPlanStatus.Approved }
            ? plan.Days.FirstOrDefault(d => d.Date == date)
            : null;
    }

    private async Task<bool> HasApprovedFullDayLeaveAsync(int studentId, DateOnly date, CancellationToken ct)
    {
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria
        {
            StudentId = studentId,
            Year = date.Year,
            Status = LeaveStatus.Approved
        }, ct);
        return leaves.Any(l => l.Hours is null && l.StartDate <= date && date <= l.EndDate);
    }

    private async Task<AttendanceComparisonDto> CompareAsync(StudentProfile student, int year, int month, CancellationToken ct)
    {
        if (year < 2000 || year > 2100 || month is < 1 or > 12)
            throw new DomainValidationException("Yıl veya ay geçersiz.");

        var plan = await _plans.GetWithDaysAsync(student.Id, year, month, ct);
        var approvedPlan = plan is { Status: WorkPlanStatus.Approved } ? plan : null;

        var attended = (await _records.SearchAsync(new AttendanceSearchCriteria { StudentId = student.Id, Year = year, Month = month }, ct))
            .Select(r => r.Date).ToHashSet();
        var absences = (await _absences.SearchAsync(new AbsenceSearchCriteria { StudentId = student.Id, Year = year, Month = month }, ct))
            .Where(a => !a.IsRevoked).Select(a => a.Date).ToHashSet();

        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var leaves = (await _leaves.SearchAsync(new LeaveSearchCriteria { StudentId = student.Id, Year = year, Status = LeaveStatus.Approved }, ct))
            .Where(l => l.StartDate <= last && l.EndDate >= first).ToList();

        var result = AttendanceComparer.Compare(approvedPlan?.Days ?? Enumerable.Empty<WorkPlanDay>(),
            attended, leaves, absences, _clock.Today);

        return result.ToDto(student.Id, year, month, approvedPlan is not null);
    }

    private static string RequireReason(string? value, string field, string message)
    {
        var reason = value?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length < 3)
            throw new DomainValidationException(field, message);
        return reason;
    }

    /// <summary>Reddedilen okutmayı denetim kaydına yazar ve kullanıcıya gösterilecek hatayı döndürür.</summary>
    private async Task<DomainValidationException> RejectAsync(string message, int? sessionId, CancellationToken ct)
    {
        await _audit.RecordAsync(AuditActions.CheckInRejected, nameof(AttendanceSession), sessionId, message, ct: ct);
        await _uow.SaveChangesAsync(ct);
        return new DomainValidationException("Token", message);
    }
}
