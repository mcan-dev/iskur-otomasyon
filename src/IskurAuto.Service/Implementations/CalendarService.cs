using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;

namespace IskurAuto.Service.Implementations;

public class CalendarService : ICalendarService
{
    private readonly IHolidayRepository _holidays;
    private readonly IExamPeriodRepository _examPeriods;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public CalendarService(IHolidayRepository holidays, IExamPeriodRepository examPeriods,
        IUnitOfWork uow, IAuditService audit, ICurrentUser current)
    {
        _holidays = holidays;
        _examPeriods = examPeriods;
        _uow = uow;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<HolidayDto>> ListHolidaysAsync(int year, CancellationToken ct = default)
    {
        RequireSignedIn();

        var holidays = await _holidays.GetBetweenAsync(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), ct);
        return holidays.Select(h => h.ToDto()).ToList();
    }

    public async Task<HolidayDto> AddHolidayAsync(HolidayRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        if (await _holidays.DateExistsAsync(request.Date, ct))
            throw new ConflictException("Bu tarih zaten resmî tatil olarak tanımlı.");

        var holiday = new Holiday { Date = request.Date, Name = request.Name.Trim() };
        await _holidays.AddAsync(holiday);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.HolidayCreated, nameof(Holiday), holiday.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return holiday.ToDto();
    }

    public async Task DeleteHolidayAsync(int id, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var holiday = await _holidays.GetByIdAsync(id)
            ?? throw new NotFoundException("Tatil kaydı bulunamadı.");

        _holidays.Remove(holiday);
        await _audit.RecordAsync(AuditActions.HolidayDeleted, nameof(Holiday), id, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExamPeriodDto>> ListExamPeriodsAsync(int year, CancellationToken ct = default)
    {
        RequireSignedIn();

        var periods = await _examPeriods.GetOverlappingAsync(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), ct);
        return periods.Select(p => p.ToDto()).ToList();
    }

    public async Task<ExamPeriodDto> AddExamPeriodAsync(ExamPeriodRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        if (request.EndDate < request.StartDate)
            throw new DomainValidationException(nameof(request.EndDate), "Bitiş tarihi başlangıçtan önce olamaz.");

        var period = new ExamPeriod { Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate };
        await _examPeriods.AddAsync(period);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.ExamPeriodCreated, nameof(ExamPeriod), period.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return period.ToDto();
    }

    public async Task DeleteExamPeriodAsync(int id, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var period = await _examPeriods.GetByIdAsync(id)
            ?? throw new NotFoundException("Sınav dönemi bulunamadı.");

        _examPeriods.Remove(period);
        await _audit.RecordAsync(AuditActions.ExamPeriodDeleted, nameof(ExamPeriod), id, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    private void RequireSignedIn()
        => _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor, UserRole.Student);
}
