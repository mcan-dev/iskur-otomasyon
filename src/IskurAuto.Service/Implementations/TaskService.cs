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

namespace IskurAuto.Service.Implementations;

public class TaskService : ITaskService
{
    private readonly IStudentTaskRepository _tasks;
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public TaskService(IStudentTaskRepository tasks, IStudentRepository students, IUnitOfWork uow,
        IClock clock, IAuditService audit, ICurrentUser current)
    {
        _tasks = tasks;
        _students = students;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<TaskDto> AssignAsync(AssignTaskRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var student = await _students.GetDetailAsync(request.StudentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        _current.EnsureCanView(student);

        var errors = new ValidationErrors();
        if (!student.IsActive)
            errors.Add(nameof(request.StudentId), "Programdan ayrılmış öğrenciye görev atanamaz.");
        if (request.DueDate < _clock.Today)
            errors.Add(nameof(request.DueDate), "Hedef tarih geçmişte olamaz.");
        errors.ThrowIfAny();

        var task = new StudentTask
        {
            StudentProfileId = student.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            AssignedDate = _clock.Today,
            DueDate = request.DueDate,
            AssignedByUserId = _current.UserId
        };
        await _tasks.AddAsync(task);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.TaskAssigned, nameof(StudentTask), task.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await _tasks.GetDetailAsync(task.Id, ct))!.ToDto(_clock.Today, withEvaluations: true);
    }

    public async Task<TaskDto> EvaluateAsync(int taskId, EvaluateTaskRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Advisor);

        var task = await _tasks.GetDetailAsync(taskId, ct)
            ?? throw new NotFoundException("Görev bulunamadı.");

        // Yalnızca öğrencinin şu anki danışmanı değerlendirebilir; diğerleri için görev yokmuş gibi davranılır.
        var student = await _students.GetDetailAsync(task.StudentProfileId, ct);
        if (student is null || student.CurrentAdvisorAssignment?.AdvisorUserId != _current.UserId)
            throw new NotFoundException("Görev bulunamadı.");

        var note = request.Note?.Trim();
        if (request.NewStatus is { } status && !Enum.IsDefined(status))
            throw new DomainValidationException(nameof(request.NewStatus), "Geçersiz görev durumu.");

        var statusChanges = request.NewStatus is { } s && s != task.Status;
        if (string.IsNullOrEmpty(note) && !statusChanges)
            throw new DomainValidationException("Not yazın veya görev durumunu değiştirin.");

        var now = _clock.UtcNow;
        task.Evaluations.Add(new TaskEvaluation
        {
            AuthorUserId = _current.UserId,
            Note = string.IsNullOrEmpty(note) ? null : note,
            NewStatus = statusChanges ? request.NewStatus : null,
            CreatedAt = now
        });
        if (statusChanges)
        {
            task.Status = request.NewStatus!.Value;
            task.StatusChangedAt = now;
        }

        await _audit.RecordAsync(AuditActions.TaskEvaluated, nameof(StudentTask), task.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return (await _tasks.GetDetailAsync(task.Id, ct))!.ToDto(_clock.Today, withEvaluations: true);
    }

    public async Task<TaskDto> GetByIdAsync(int taskId, CancellationToken ct = default)
    {
        var task = await _tasks.GetDetailAsync(taskId, ct)
            ?? throw new NotFoundException("Görev bulunamadı.");

        var student = await _students.GetDetailAsync(task.StudentProfileId, ct);
        if (student is null || !_current.CanView(student))
            throw new NotFoundException("Görev bulunamadı.");

        return task.ToDto(_clock.Today, withEvaluations: true);
    }

    public async Task<IReadOnlyList<TaskDto>> SearchAsync(TaskQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new StudentTaskSearchCriteria
        {
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            Status = query.Status,
            Year = query.Year,
            OverdueBefore = query.OverdueOnly ? _clock.Today : null
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

        var tasks = await _tasks.SearchAsync(criteria, ct);
        return tasks.Select(t => t.ToDto(_clock.Today)).ToList();
    }

    public async Task<IReadOnlyList<TaskDto>> ListMineAsync(StudentTaskStatus? status, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await _students.GetDetailByUserIdAsync(_current.UserId, ct)
            ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

        var tasks = await _tasks.SearchAsync(new StudentTaskSearchCriteria { StudentId = student.Id, Status = status }, ct);
        return tasks.Select(t => t.ToDto(_clock.Today)).ToList();
    }
}
