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

public class AbsenceService : IAbsenceService
{
    private readonly IAbsenceRepository _absences;
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AbsenceService(IAbsenceRepository absences, IStudentRepository students, IUnitOfWork uow,
        IClock clock, IAuditService audit, ICurrentUser current)
    {
        _absences = absences;
        _students = students;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<AbsenceDto> RecordAsync(RecordAbsenceRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var student = await _students.GetDetailAsync(request.StudentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");

        if (request.Date > _clock.Today)
            throw new DomainValidationException(nameof(request.Date), "Gelecekteki bir gün için devamsızlık girilemez.");
        if (!Enum.IsDefined(request.Type))
            throw new DomainValidationException(nameof(request.Type), "Geçersiz devamsızlık türü.");
        if (await _absences.ActiveExistsAsync(student.Id, request.Date, ct))
            throw new ConflictException("Bu öğrenci için bu tarihte zaten devamsızlık kaydı var.");

        var absence = new Absence
        {
            StudentProfileId = student.Id,
            Date = request.Date,
            Type = request.Type,
            Note = request.Note?.Trim(),
            RecordedByUserId = _current.UserId
        };
        await _absences.AddAsync(absence);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.AbsenceRecorded, nameof(Absence), absence.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await _absences.GetDetailAsync(absence.Id, ct))!.ToDto();
    }

    public async Task<AbsenceDto> RevokeAsync(int absenceId, RevokeAbsenceRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var absence = await _absences.GetDetailAsync(absenceId, ct)
            ?? throw new NotFoundException("Devamsızlık kaydı bulunamadı.");

        if (absence.IsRevoked)
            throw new ConflictException("Bu kayıt zaten geri alınmış.");

        absence.RevokedAt = _clock.UtcNow;
        absence.RevokedByUserId = _current.UserId;
        absence.RevokeReason = request.Reason.Trim();

        await _audit.RecordAsync(AuditActions.AbsenceRevoked, nameof(Absence), absence.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return (await _absences.GetDetailAsync(absence.Id, ct))!.ToDto();
    }

    public async Task<IReadOnlyList<AbsenceDto>> SearchAsync(AbsenceQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new AbsenceSearchCriteria
        {
            Year = query.Year,
            Month = query.Month,
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId
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

        var absences = await _absences.SearchAsync(criteria, ct);
        return absences.Select(a => a.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<AbsenceDto>> ListMineAsync(int? year, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await _students.GetDetailByUserIdAsync(_current.UserId, ct)
            ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

        var absences = await _absences.SearchAsync(new AbsenceSearchCriteria { StudentId = student.Id, Year = year }, ct);
        return absences.Select(a => a.ToDto()).ToList();
    }
}
