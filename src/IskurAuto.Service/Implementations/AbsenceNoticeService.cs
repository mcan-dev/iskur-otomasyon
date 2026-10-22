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

public class AbsenceNoticeService : IAbsenceNoticeService
{
    private readonly IAbsenceNoticeRepository _notices;
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AbsenceNoticeService(IAbsenceNoticeRepository notices, IStudentRepository students, IUnitOfWork uow,
        IClock clock, IAuditService audit, ICurrentUser current)
    {
        _notices = notices;
        _students = students;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<AbsenceNoticeDto> SendAsync(SendAbsenceNoticeRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Advisor);

        var student = await _students.GetDetailAsync(request.StudentId, ct);
        if (student is null || student.CurrentAdvisorAssignment?.AdvisorUserId != _current.UserId)
            throw new NotFoundException("Öğrenci bulunamadı.");

        if (request.Date > _clock.Today)
            throw new DomainValidationException(nameof(request.Date), "Gelecekteki bir gün için bildirim gönderilemez.");
        if (await _notices.ExistsAsync(_current.UserId, student.Id, request.Date, ct))
            throw new ConflictException("Bu öğrenci ve gün için zaten bildirim gönderdiniz.");

        var notice = new AbsenceNotice
        {
            StudentProfileId = student.Id,
            Date = request.Date,
            Message = request.Message.Trim(),
            AdvisorUserId = _current.UserId,
            CreatedAt = _clock.UtcNow
        };
        await _notices.AddAsync(notice);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.AbsenceNoticeSent, nameof(AbsenceNotice), notice.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await _notices.GetDetailAsync(notice.Id, ct))!.ToDto();
    }

    public async Task<AbsenceNoticeDto> MarkReadAsync(int noticeId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.DepartmentHead);

        var notice = await _notices.GetDetailAsync(noticeId, ct);
        if (notice is null || notice.StudentProfile.DepartmentId != _current.DepartmentId)
            throw new NotFoundException("Bildirim bulunamadı.");

        if (!notice.IsRead)
        {
            notice.ReadAt = _clock.UtcNow;
            notice.ReadByUserId = _current.UserId;
            await _audit.RecordAsync(AuditActions.AbsenceNoticeRead, nameof(AbsenceNotice), notice.Id, ct: ct);
            await _uow.SaveChangesAsync(ct);
        }

        return (await _notices.GetDetailAsync(notice.Id, ct))!.ToDto();
    }

    public async Task<IReadOnlyList<AbsenceNoticeDto>> SearchAsync(AbsenceNoticeQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new AbsenceNoticeSearchCriteria
        {
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            UnreadOnly = query.UnreadOnly,
            Year = query.Year,
            Month = query.Month
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

        var notices = await _notices.SearchAsync(criteria, ct);
        return notices.Select(n => n.ToDto()).ToList();
    }
}
