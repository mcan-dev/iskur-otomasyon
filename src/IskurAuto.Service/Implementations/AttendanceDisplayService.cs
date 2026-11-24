using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Attendance;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;

namespace IskurAuto.Service.Implementations;

/// <summary>
/// Fakültedeki monitörlerde gösterilen mesai karekodu. Sekreterliğin belirlediği sabah (giriş) ve akşam (çıkış)
/// saat aralıklarında, hafta içi ve tatil olmayan günlerde fakülte geneli bir devam oturumu kendiliğinden açılır;
/// karekod 30 saniyede bir yenilenir. Öğrenci yalnızca onaylı planındaki çalışma gününde okutabilir.
/// </summary>
public class AttendanceDisplayService : IAttendanceDisplayService
{
    private const string Prefix = "Attendance.";
    private const string MorningStartKey = Prefix + "MorningStart";
    private const string MorningEndKey = Prefix + "MorningEnd";
    private const string EveningStartKey = Prefix + "EveningStart";
    private const string EveningEndKey = Prefix + "EveningEnd";

    public const string MorningTitle = "Sabah mesai girişi";
    public const string EveningTitle = "Akşam mesai çıkışı";

    private readonly ISystemSettingRepository _settings;
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IHolidayRepository _holidays;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AttendanceDisplayService(ISystemSettingRepository settings, IAttendanceSessionRepository sessions,
        IHolidayRepository holidays, IUnitOfWork uow, IClock clock, IAuditService audit, ICurrentUser current)
    {
        _settings = settings;
        _sessions = sessions;
        _holidays = holidays;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<AttendanceScheduleDto> GetScheduleAsync(CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor, UserRole.Student);
        return await LoadAsync(ct);
    }

    public async Task<AttendanceScheduleDto> UpdateScheduleAsync(AttendanceScheduleDto request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var errors = new ValidationErrors();
        if (request.MorningEnd <= request.MorningStart)
            errors.Add(nameof(request.MorningEnd), "Sabah karekod bitiş saati başlangıçtan sonra olmalıdır.");
        if (request.EveningEnd <= request.EveningStart)
            errors.Add(nameof(request.EveningEnd), "Akşam karekod bitiş saati başlangıçtan sonra olmalıdır.");
        if (request.EveningStart < request.MorningEnd)
            errors.Add(nameof(request.EveningStart), "Akşam karekodu sabah karekodu bittikten sonra başlamalıdır.");
        errors.ThrowIfAny();

        var stored = (await _settings.GetByPrefixAsync(Prefix, ct)).ToDictionary(s => s.Key);
        await Upsert(stored, MorningStartKey, request.MorningStart);
        await Upsert(stored, MorningEndKey, request.MorningEnd);
        await Upsert(stored, EveningStartKey, request.EveningStart);
        await Upsert(stored, EveningEndKey, request.EveningEnd);

        await _audit.RecordAsync(AuditActions.AttendanceScheduleChanged, nameof(SystemSetting),
            details: $"Sabah {request.MorningStart:HH\\:mm}-{request.MorningEnd:HH\\:mm}, akşam {request.EveningStart:HH\\:mm}-{request.EveningEnd:HH\\:mm}",
            ct: ct);
        await _uow.SaveChangesAsync(ct);

        return request;
    }

    public async Task<AttendanceDisplayDto> GetDisplayAsync(CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var schedule = await LoadAsync(ct);
        var localNow = _clock.LocalNow;
        var today = _clock.Today;
        var time = TimeOnly.FromDateTime(localNow);

        var isWorkingDay = today.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
            && !(await _holidays.GetBetweenAsync(today, today, ct)).Any();

        if (!isWorkingDay)
            return Closed(schedule, "Bugün mesai yok (hafta sonu veya resmî tatil).", null);

        DisplayWindow window;
        string title;
        TimeOnly start, end;
        if (time >= schedule.MorningStart && time <= schedule.MorningEnd)
            (window, title, start, end) = (DisplayWindow.Morning, MorningTitle, schedule.MorningStart, schedule.MorningEnd);
        else if (time >= schedule.EveningStart && time <= schedule.EveningEnd)
            (window, title, start, end) = (DisplayWindow.Evening, EveningTitle, schedule.EveningStart, schedule.EveningEnd);
        else
        {
            var next = time < schedule.MorningStart ? $"Sabah karekodu bugün {schedule.MorningStart:HH\\:mm}'de açılır."
                : time < schedule.EveningStart ? $"Akşam karekodu bugün {schedule.EveningStart:HH\\:mm}'de açılır."
                : "Bugünkü karekod saatleri sona erdi.";
            return Closed(schedule, "Şu an karekod gösterilmiyor.", next);
        }

        var session = await EnsureSessionAsync(today, title, start, end, ct);
        var now = _clock.UtcNow;

        return new AttendanceDisplayDto
        {
            Window = window,
            Title = title,
            WindowStart = start,
            WindowEnd = end,
            Schedule = schedule,
            Qr = new QrCodeDto
            {
                SessionId = session.Id,
                Token = QrToken.Create(session.Id, session.SecretKey, now),
                RefreshAtUtc = QrToken.RefreshAt(now),
                WindowSeconds = QrToken.WindowSeconds
            }
        };
    }

    /// <summary>Bugünün bu penceresi için fakülte geneli oturumu bulur; yoksa açar.</summary>
    private async Task<AttendanceSession> EnsureSessionAsync(DateOnly date, string title, TimeOnly start, TimeOnly end, CancellationToken ct)
    {
        var existing = (await _sessions.ListAsync(date, date, null, ct))
            .FirstOrDefault(s => !s.IsClosed && s.DepartmentId is null && s.Title == title
                && s.StartTime == start && s.EndTime == end);
        if (existing is not null)
            return existing;

        var session = new AttendanceSession
        {
            Date = date,
            StartTime = start,
            EndTime = end,
            Title = title,
            DepartmentId = null,
            SecretKey = QrToken.NewSecret(),
            CreatedByUserId = _current.UserId
        };
        await _sessions.AddAsync(session);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.SessionCreated, nameof(AttendanceSession), session.Id,
            $"{title} karekodu otomatik açıldı", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return session;
    }

    private static AttendanceDisplayDto Closed(AttendanceScheduleDto schedule, string title, string? next) => new()
    {
        Window = DisplayWindow.Closed,
        Title = title,
        NextWindow = next,
        Schedule = schedule
    };

    private async Task Upsert(Dictionary<string, SystemSetting> stored, string key, TimeOnly value)
    {
        var text = value.ToString("HH\\:mm");
        if (stored.TryGetValue(key, out var existing))
            existing.Value = text;
        else
            await _settings.AddAsync(new SystemSetting { Key = key, Value = text });
    }

    private async Task<AttendanceScheduleDto> LoadAsync(CancellationToken ct)
    {
        var values = (await _settings.GetByPrefixAsync(Prefix, ct)).ToDictionary(s => s.Key, s => s.Value);
        var dto = new AttendanceScheduleDto();
        dto.MorningStart = Time(values, MorningStartKey, dto.MorningStart);
        dto.MorningEnd = Time(values, MorningEndKey, dto.MorningEnd);
        dto.EveningStart = Time(values, EveningStartKey, dto.EveningStart);
        dto.EveningEnd = Time(values, EveningEndKey, dto.EveningEnd);
        return dto;
    }

    private static TimeOnly Time(Dictionary<string, string> values, string key, TimeOnly fallback)
        => values.TryGetValue(key, out var raw) && TimeOnly.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed : fallback;
}
