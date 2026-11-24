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

/// <summary>
/// İzinler. Öğrenci izin günlerini o ayın onaylı çalışma planından seçer; izin onay beklemeden geçerli olur ve
/// sekreterlik bilgilendirilir. Sekreterlik izni onaylamaz, gerekirse iptal eder ve öğrencinin toplam hakkını ayarlar.
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaves;
    private readonly IAbsenceRepository _absences;
    private readonly IStudentRepository _students;
    private readonly IWorkPlanRepository _plans;
    private readonly ILeavePolicyProvider _policies;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public LeaveService(
        ILeaveRepository leaves, IAbsenceRepository absences, IStudentRepository students,
        IWorkPlanRepository plans, ILeavePolicyProvider policies, IUnitOfWork uow, IClock clock,
        IAuditService audit, ICurrentUser current)
    {
        _leaves = leaves;
        _absences = absences;
        _students = students;
        _plans = plans;
        _policies = policies;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    // ─── Öğrenci ──────────────────────────────────────────────────────────────

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

    public async Task<LeaveCalendarDto> GetMyCalendarAsync(int year, int month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);
        EnsureValidMonth(year, month);

        var student = await LoadMineAsync(ct);
        var plan = await ApprovedPlanAsync(student.Id, year, month, ct);
        var onLeave = await LeaveDaysAsync(student.Id, year, month, ct);
        var today = _clock.Today;

        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var workDays = plan?.Days.Select(d => d.Date).ToHashSet() ?? new HashSet<DateOnly>();

        var days = new List<LeaveCalendarDayDto>();
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            var isWorkDay = workDays.Contains(day);
            var isOnLeave = onLeave.Contains(day);
            days.Add(new LeaveCalendarDayDto
            {
                Date = day,
                IsWorkDay = isWorkDay,
                IsOnLeave = isOnLeave,
                CanSelect = student.IsActive && isWorkDay && !isOnLeave && day >= today
            });
        }

        return new LeaveCalendarDto
        {
            Year = year,
            Month = month,
            HasApprovedPlan = plan is not null,
            Days = days,
            Balance = await BalanceAsync(student.Id, year, ct)
        };
    }

    public async Task<IReadOnlyList<LeaveDto>> RequestAsync(CreateLeaveRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        if (!student.IsActive)
            throw new ConflictException("Programda aktif olmadığınız için izin talep edemezsiniz.");

        var days = (request.Days ?? new List<DateOnly>()).Distinct().OrderBy(d => d).ToList();
        if (days.Count == 0)
            throw new DomainValidationException(nameof(request.Days), "İzin almak istediğiniz en az bir çalışma gününü seçin.");

        var year = days[0].Year;
        var month = days[0].Month;
        if (days.Any(d => d.Year != year || d.Month != month))
            throw new DomainValidationException(nameof(request.Days), "Bir talepte yalnızca aynı ayın günleri seçilebilir.");

        var plan = await ApprovedPlanAsync(student.Id, year, month, ct)
            ?? throw new ConflictException($"{TurkishText.MonthYear(year, month)} için onaylanmış bir çalışma planınız yok; izin yalnızca çalışma günleri için alınabilir.");

        var workDays = plan.Days.Select(d => d.Date).ToHashSet();
        var alreadyOnLeave = await LeaveDaysAsync(student.Id, year, month, ct);
        var today = _clock.Today;

        var errors = new List<string>();
        foreach (var day in days)
        {
            if (!workDays.Contains(day))
                errors.Add($"{Format(day)} çalışma gününüz değil; izin yalnızca çalışma günleri için alınabilir.");
            else if (day < today)
                errors.Add($"{Format(day)} geçmiş bir gün; geçmiş günler için izin alınamaz.");
            else if (alreadyOnLeave.Contains(day))
                errors.Add($"{Format(day)} için zaten izniniz var.");
        }
        if (errors.Count > 0)
            throw new DomainValidationException(new Dictionary<string, string[]> { [nameof(request.Days)] = errors.ToArray() });

        // Öğrenci ay içinde en az bir gün çalışmak zorundadır: ayın bütün çalışma günleri izinle kapatılamaz.
        var remainingWorkDays = workDays.Count(d => !alreadyOnLeave.Contains(d) && !days.Contains(d));
        if (remainingWorkDays < 1)
            throw new ConflictException("Bir ay içinde en az bir gün çalışmanız gerekir; ayın tüm çalışma günleri için izin alınamaz.");

        var balance = await BalanceAsync(student.Id, year, ct);
        if (balance.Remaining < days.Count)
            throw new ConflictException(balance.Remaining <= 0
                ? "İzin hakkınız kalmadı. Fakülte sekreterliği ile görüşün."
                : $"İzin hakkınız {balance.Remaining:0.##} gün; {days.Count} gün seçtiniz.");

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var created = days.Select(day => new LeaveRequest
        {
            StudentProfileId = student.Id,
            Status = LeaveStatus.Approved,
            StartDate = day,
            EndDate = day,
            DaysCounted = 1,
            Description = description,
            AppliedOn = today
        }).ToList();

        foreach (var leave in created)
            await _leaves.AddAsync(leave);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        foreach (var leave in created)
            await _audit.RecordAsync(AuditActions.LeaveRequested, nameof(LeaveRequest), leave.Id,
                $"{Format(leave.StartDate)} izin günü (sekreterliğe bildirildi)", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var result = new List<LeaveDto>();
        foreach (var leave in created)
            result.Add((await _leaves.GetDetailAsync(leave.Id, ct))!.ToDto());
        return result;
    }

    public async Task<LeaveDto> CancelMineAsync(int leaveId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        var leave = await _leaves.GetDetailAsync(leaveId, ct);
        if (leave is null || leave.StudentProfileId != student.Id)
            throw new NotFoundException("İzin bulunamadı.");

        if (leave.StartDate < _clock.Today)
            throw new ConflictException("Geçmiş bir günün izni öğrenci tarafından iptal edilemez.");

        return await MarkCancelledAsync(leave, null, ct);
    }

    // ─── Personel ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LeaveDto>> SearchAsync(LeaveSearchQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new LeaveSearchCriteria
        {
            Year = query.Year,
            Month = query.Year is null ? null : query.Month,
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

        var leave = await _leaves.GetDetailAsync(leaveId, ct)
            ?? throw new NotFoundException("İzin bulunamadı.");
        if (!_current.CanView(leave.StudentProfile))
            throw new NotFoundException("İzin bulunamadı.");

        return leave.ToDto();
    }

    public async Task<LeaveDto> CancelAsync(int leaveId, CancelLeaveRequest request, CancellationToken ct = default)
    {
        // İzni yalnızca fakülte sekreterliği iptal eder; onaylama diye bir adım yoktur.
        _current.RequireRole(UserRole.FacultyAdmin);

        var leave = await _leaves.GetDetailAsync(leaveId, ct)
            ?? throw new NotFoundException("İzin bulunamadı.");

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        return await MarkCancelledAsync(leave, note, ct);
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

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<LeaveDto> MarkCancelledAsync(LeaveRequest leave, string? note, CancellationToken ct)
    {
        if (leave.Status == LeaveStatus.Cancelled)
            throw new ConflictException("Bu izin zaten iptal edilmiş.");

        leave.Status = LeaveStatus.Cancelled;
        leave.CancelledByUserId = _current.UserId;
        leave.CancelledAt = _clock.UtcNow;
        leave.CancelNote = note;

        await _audit.RecordAsync(AuditActions.LeaveCancelled, nameof(LeaveRequest), leave.Id,
            $"{Format(leave.StartDate)} izni iptal edildi", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return (await _leaves.GetDetailAsync(leave.Id, ct))!.ToDto();
    }

    private async Task<MonthlyWorkPlan?> ApprovedPlanAsync(int studentId, int year, int month, CancellationToken ct)
    {
        var plan = await _plans.GetWithDaysAsync(studentId, year, month, ct);
        return plan is { Status: WorkPlanStatus.Approved } ? plan : null;
    }

    private async Task<HashSet<DateOnly>> LeaveDaysAsync(int studentId, int year, int month, CancellationToken ct)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria
        {
            StudentId = studentId,
            Year = year,
            Status = LeaveStatus.Approved
        }, ct);

        var result = new HashSet<DateOnly>();
        foreach (var leave in leaves)
            for (var day = leave.StartDate; day <= leave.EndDate; day = day.AddDays(1))
                if (day >= first && day <= last)
                    result.Add(day);
        return result;
    }

    private async Task<LeaveBalanceDto> BalanceAsync(int studentId, int year, CancellationToken ct)
    {
        var policy = await _policies.GetAsync(ct);
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria { StudentId = studentId, Year = year }, ct);
        var absences = await _absences.SearchAsync(new AbsenceSearchCriteria { StudentId = studentId, Year = year }, ct);

        var student = await _students.GetByIdAsync(studentId);

        return LeaveBalanceCalculator.Calculate(policy, leaves, absences, student?.LeaveDayAllowance).ToDto(year);
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

    private static void EnsureValidMonth(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new DomainValidationException("Month", "Geçerli bir yıl ve ay giriniz.");
    }

    private static string Format(DateOnly date) => date.ToString("dd.MM.yyyy");
}
