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

namespace IskurAuto.Service.Implementations;

/// <summary>
/// İlişik kesme kararının kaydı. Sistem karar vermez: yetkili kişi karar verir, gerekçe yazar;
/// bu servis kararı saklar ve öğrencinin program katılımını sonlandırır.
/// </summary>
public class TerminationService : ITerminationService
{
    private readonly ITerminationRepository _decisions;
    private readonly IStudentRepository _students;
    private readonly IStudentService _studentService;
    private readonly ILeaveRepository _leaves;
    private readonly IAbsenceRepository _absences;
    private readonly ILeavePolicyProvider _policy;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public TerminationService(ITerminationRepository decisions, IStudentRepository students, IStudentService studentService,
        ILeaveRepository leaves, IAbsenceRepository absences, ILeavePolicyProvider policy,
        IUnitOfWork uow, IClock clock, IAuditService audit, ICurrentUser current)
    {
        _decisions = decisions;
        _students = students;
        _studentService = studentService;
        _leaves = leaves;
        _absences = absences;
        _policy = policy;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<TerminationDto> RecordAsync(RecordTerminationRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        if (!Enum.IsDefined(request.Basis))
            throw new DomainValidationException(nameof(request.Basis), "Geçersiz karar dayanağı.");
        var note = request.Note?.Trim();
        if (string.IsNullOrEmpty(note) || note.Length < 10)
            throw new DomainValidationException(nameof(request.Note), "Karar gerekçesini en az 10 karakterle yazmalısınız.");

        var student = await _students.GetDetailAsync(request.StudentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        var participation = student.CurrentParticipation
            ?? throw new ConflictException("Öğrenci zaten pasif durumda.");

        var today = _clock.Today;
        var decisionDate = request.DecisionDate ?? today;
        if (decisionDate > today)
            throw new DomainValidationException(nameof(request.DecisionDate), "Karar tarihi gelecekte olamaz.");
        if (decisionDate < participation.StartDate)
            throw new DomainValidationException(nameof(request.DecisionDate), "Karar tarihi program başlangıcından önce olamaz.");

        var balance = await BalanceAsync(student.Id, decisionDate.Year, ct);

        await using var tx = await _uow.BeginTransactionAsync(ct);

        await _studentService.EndParticipationAsync(student.Id, new EndParticipationRequest
        {
            EndDate = decisionDate,
            Reason = $"İlişik kesme ({BasisText(request.Basis)}): {note}"
        }, ct);

        var decision = new TerminationDecision
        {
            StudentProfileId = student.Id,
            Basis = request.Basis,
            DecisionDate = decisionDate,
            Note = note,
            LeaveDaysUsed = balance.Used,
            UnexcusedAbsenceDays = (int)balance.UnexcusedAbsenceDays,
            DecidedByUserId = _current.UserId,
            CreatedAt = _clock.UtcNow
        };
        await _decisions.AddAsync(decision);
        await _uow.SaveChangesAsync(ct);

        await _audit.RecordAsync(AuditActions.TerminationRecorded, nameof(TerminationDecision), decision.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToDto((await _decisions.GetDetailAsync(decision.Id, ct))!);
    }

    public async Task<IReadOnlyList<TerminationDto>> SearchAsync(TerminationQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var criteria = new TerminationSearchCriteria { StudentId = query.StudentId, DepartmentId = query.DepartmentId, Year = query.Year };
        if (_current.Role == UserRole.DepartmentHead)
            criteria.DepartmentId = _current.DepartmentId ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış.");

        var decisions = await _decisions.SearchAsync(criteria, ct);
        return decisions.Select(ToDto).ToList();
    }

    private async Task<LeaveBalance> BalanceAsync(int studentId, int year, CancellationToken ct)
    {
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria { StudentId = studentId, Year = year }, ct);
        var absences = await _absences.SearchAsync(new AbsenceSearchCriteria { StudentId = studentId, Year = year }, ct);
        return LeaveBalanceCalculator.Calculate(await _policy.GetAsync(ct), leaves, absences);
    }

    private static string BasisText(TerminationBasis basis) => basis switch
    {
        TerminationBasis.LeaveLimitExceeded => "izin hakkının aşılması",
        TerminationBasis.AbsenceLimitExceeded => "devamsızlık sınırının aşılması",
        TerminationBasis.StudentRequest => "öğrencinin talebi",
        _ => "diğer"
    };

    private static TerminationDto ToDto(TerminationDecision d) => new()
    {
        Id = d.Id,
        StudentId = d.StudentProfileId,
        StudentName = d.StudentProfile.User.FullName,
        DepartmentName = d.StudentProfile.Department.Name,
        Basis = d.Basis,
        DecisionDate = d.DecisionDate,
        Note = d.Note,
        LeaveDaysUsed = d.LeaveDaysUsed,
        UnexcusedAbsenceDays = d.UnexcusedAbsenceDays,
        DecidedByName = d.DecidedByUser.FullName,
        CreatedAt = d.CreatedAt
    };
}
