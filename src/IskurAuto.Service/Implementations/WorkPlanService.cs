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
using IskurAuto.Service.Mapping;
using IskurAuto.Service.WorkPlans;

namespace IskurAuto.Service.Implementations;

public class WorkPlanService : IWorkPlanService
{
    private readonly IWorkPlanRepository _plans;
    private readonly IStudentRepository _students;
    private readonly IHolidayRepository _holidays;
    private readonly IExamPeriodRepository _examPeriods;
    private readonly IStudentCourseDayRepository _courseDays;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;
    private readonly WorkPlanSettings _settings;

    public WorkPlanService(
        IWorkPlanRepository plans, IStudentRepository students, IHolidayRepository holidays,
        IExamPeriodRepository examPeriods, IStudentCourseDayRepository courseDays, IUnitOfWork uow,
        IClock clock, IAuditService audit, ICurrentUser current, WorkPlanSettings settings)
    {
        _plans = plans;
        _students = students;
        _holidays = holidays;
        _examPeriods = examPeriods;
        _courseDays = courseDays;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
        _settings = settings;
    }

    public async Task<WorkPlanCalendarDto> GetMyCalendarAsync(int year, int month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);
        EnsureValidMonth(year, month);

        var student = await LoadMineAsync(ct);
        return await BuildCalendarAsync(student, year, month, ct);
    }

    public async Task<WorkPlanDto> GetMyPlanAsync(int year, int month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);
        EnsureValidMonth(year, month);

        var student = await LoadMineAsync(ct);
        var plan = await _plans.GetWithDaysAsync(student.Id, year, month, ct)
            ?? throw new NotFoundException("Bu ay için kayıtlı bir planınız yok.");

        return plan.ToDto(_settings);
    }

    public async Task<IReadOnlyList<WorkPlanDto>> ListMyPlansAsync(CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        var plans = await _plans.ListByStudentAsync(student.Id, ct);
        return plans.Select(p => p.ToDto(_settings)).ToList();
    }

    public async Task<WorkPlanDto> SaveMyPlanAsync(SaveWorkPlanRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);
        EnsureValidMonth(request.Year, request.Month);

        var student = await LoadMineAsync(ct);
        CourseDayService.EnsureIbanSaved(student);
        if (!student.IsActive)
            throw new ConflictException("Programda aktif olmadığınız için plan oluşturamazsınız.");

        var existing = await _plans.GetWithDaysAsync(student.Id, request.Year, request.Month, ct);
        EnsureStudentMayEdit(existing, request.Year, request.Month);

        return await SaveAsync(student, existing, request, ct);
    }

    public async Task<IReadOnlyList<WorkPlanDto>> SearchAsync(WorkPlanSearchQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);
        EnsureValidMonth(query.Year, query.Month);

        var criteria = new WorkPlanSearchCriteria
        {
            Year = query.Year,
            Month = query.Month,
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            Status = query.Status
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

        var plans = await _plans.SearchAsync(criteria, ct);
        return plans.Select(p => p.ToDto(_settings)).ToList();
    }

    public async Task<WorkPlanDto> GetByIdAsync(int planId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor, UserRole.Student);

        var plan = await _plans.GetDetailAsync(planId, ct)
            ?? throw new NotFoundException("Plan bulunamadı.");
        EnsureCanView(plan.StudentProfile);

        return plan.ToDto(_settings);
    }

    public async Task<WorkPlanCalendarDto> GetStudentCalendarAsync(int studentId, int year, int month, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);
        EnsureValidMonth(year, month);

        var student = await LoadAsync(studentId, ct);
        return await BuildCalendarAsync(student, year, month, ct);
    }

    public async Task<WorkPlanDto> SaveForStudentAsync(int studentId, SaveWorkPlanRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        EnsureValidMonth(request.Year, request.Month);

        var student = await LoadAsync(studentId, ct);
        var existing = await _plans.GetWithDaysAsync(student.Id, request.Year, request.Month, ct);

        return await SaveAsync(student, existing, request, ct);
    }

    public async Task<WorkPlanDto> ReviewAsync(int planId, ReviewWorkPlanRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var plan = await _plans.GetDetailAsync(planId, ct)
            ?? throw new NotFoundException("Plan bulunamadı.");

        var note = request.Note?.Trim();
        if (!request.Approve && string.IsNullOrEmpty(note))
            throw new DomainValidationException(nameof(request.Note), "Reddederken gerekçe yazmalısınız.");

        plan.Status = request.Approve ? WorkPlanStatus.Approved : WorkPlanStatus.Rejected;
        plan.ReviewedByUserId = _current.UserId;
        plan.ReviewedAt = _clock.UtcNow;
        plan.ReviewNote = note;

        await _audit.RecordAsync(request.Approve ? AuditActions.WorkPlanApproved : AuditActions.WorkPlanRejected,
            nameof(MonthlyWorkPlan), plan.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return (await _plans.GetDetailAsync(planId, ct))!.ToDto(_settings);
    }

    private async Task<WorkPlanDto> SaveAsync(StudentProfile student, MonthlyWorkPlan? plan, SaveWorkPlanRequest request, CancellationToken ct)
    {
        var month = await LoadMonthAsync(student, request.Year, request.Month, ct);

        var days = request.Days
            .Select(d => new PlannedDay(d.Date, d.Note?.Trim()))
            .ToList();

        var errors = WorkPlanRules.Validate(month, days, _settings);
        if (errors.Count > 0)
            throw new DomainValidationException(new Dictionary<string, string[]> { ["Days"] = errors.ToArray() });

        if (plan is null)
        {
            plan = new MonthlyWorkPlan { StudentProfileId = student.Id, Year = request.Year, Month = request.Month };
            await _plans.AddAsync(plan);
        }

        plan.Status = WorkPlanStatus.Pending;
        plan.StudentNote = request.StudentNote?.Trim();
        plan.LastSavedAt = _clock.UtcNow;
        plan.ReviewedByUserId = null;
        plan.ReviewedAt = null;
        plan.ReviewNote = null;

        ApplyDays(plan, days, month);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.WorkPlanSaved, nameof(MonthlyWorkPlan), plan.Id,
            $"{request.Year}-{request.Month:00}, {days.Count} gün", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await _plans.GetDetailAsync(plan.Id, ct))!.ToDto(_settings);
    }

    private void ApplyDays(MonthlyWorkPlan plan, List<PlannedDay> days, PlanMonth month)
    {
        var requested = days.ToDictionary(d => d.Date);

        _plans.RemoveDays(plan.Days.Where(d => !requested.ContainsKey(d.Date)).ToList());

        foreach (var day in days)
        {
            var entity = plan.Days.FirstOrDefault(d => d.Date == day.Date);
            if (entity is null)
            {
                entity = new WorkPlanDay { Date = day.Date };
                plan.Days.Add(entity);
            }

            entity.Note = day.Note;
            entity.IsLessonDay = month.IsLessonDay(day.Date);
        }
    }

    private async Task<WorkPlanCalendarDto> BuildCalendarAsync(StudentProfile student, int year, int month, CancellationToken ct)
    {
        var planMonth = await LoadMonthAsync(student, year, month, ct);
        var plan = await _plans.GetWithDaysAsync(student.Id, year, month, ct);

        return new WorkPlanCalendarDto
        {
            Year = year,
            Month = month,
            PlanStatus = plan?.Status,
            CanEdit = student.IsActive && CanStudentEdit(plan, year, month),
            EditDeadline = DeadlineOf(year, month),
            Days = planMonth.ToCalendar(plan)
        };
    }

    private async Task<PlanMonth> LoadMonthAsync(StudentProfile student, int year, int month, CancellationToken ct)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        var holidays = await _holidays.GetBetweenAsync(first, last, ct);
        var examPeriods = await _examPeriods.GetOverlappingAsync(first.AddDays(-7), last.AddDays(7), ct);
        var courseDays = await _courseDays.GetByStudentAsync(student.Id, ct);
        var periods = student.Participations.Select(p => new ProgramPeriod(p.StartDate, p.EndDate));

        return new PlanMonth(year, month, holidays.Select(h => h.Date), examPeriods,
            courseDays, periods);
    }

    private void EnsureStudentMayEdit(MonthlyWorkPlan? plan, int year, int month)
    {
        if (plan?.Status == WorkPlanStatus.Approved)
            throw new ConflictException("Onaylanmış plan değiştirilemez.");

        if (!CanStudentEdit(plan, year, month))
            throw new ConflictException("Bu ay için plan düzenleme süresi dolmuş veya henüz başlamamış.");
    }

    private bool CanStudentEdit(MonthlyWorkPlan? plan, int year, int month)
    {
        if (plan?.Status == WorkPlanStatus.Approved) return false;
        if (plan?.Status == WorkPlanStatus.Rejected) return true;

        var today = _clock.Today;
        var opensOn = new DateOnly(year, month, 1).AddMonths(-1);
        return today >= opensOn && today <= DeadlineOf(year, month);
    }

    private DateOnly DeadlineOf(int year, int month)
    {
        var day = Math.Min(_settings.EditDeadlineDay, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    private static void EnsureValidMonth(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new DomainValidationException("Month", "Geçerli bir yıl ve ay giriniz.");
    }

    private async Task<StudentProfile> LoadMineAsync(CancellationToken ct)
        => await _students.GetDetailByUserIdAsync(_current.UserId, ct)
           ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

    private async Task<StudentProfile> LoadAsync(int studentId, CancellationToken ct)
    {
        var student = await _students.GetDetailAsync(studentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        EnsureCanView(student);
        return student;
    }

    private void EnsureCanView(StudentProfile student) => _current.EnsureCanView(student);
}
