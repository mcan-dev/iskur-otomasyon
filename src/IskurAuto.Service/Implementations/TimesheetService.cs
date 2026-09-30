using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IskurAuto.Service.Implementations;

/// <summary>
/// <see cref="ITimesheetService"/> arayüzünün uygulaması.
/// Maker-Checker iş akışını, yetki kontrollerini ve statü geçişlerini yönetir.
/// </summary>
public class TimesheetService : ITimesheetService
{
    private readonly IskurAutoDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;

    public TimesheetService(IskurAutoDbContext context, IServiceScopeFactory scopeFactory)
    {
        _context      = context      ?? throw new ArgumentNullException(nameof(context));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    // ─── Maker (Fakülte Sekreteri) Operasyonları ─────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// İş Kuralları:
    /// 1. Yapılan sekreterin kendi fakültesine ait öğrenci için işlem yapıp yapmadığı kontrol edilir.
    /// 2. Aynı öğrenci için aynı ay/yıl kombinasyonunda kayıt zaten varsa hata fırlatılır.
    /// 3. Status otomatik olarak <c>PendingSksApproval</c> olarak atanır; Maker bunu değiştiremez.
    /// </remarks>
    public async Task<TimesheetResultDto> CreateTimesheetAsync(TimesheetCreateDto dto, int makerUserId)
    {
        // 1. Maker kullanıcısını ve fakülte bağlantısını doğrula
        var makerUser = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == makerUserId && u.IsActive)
            ?? throw new InvalidOperationException($"ID={makerUserId} olan aktif kullanıcı bulunamadı.");

        // 2. Öğrencinin mevcut olduğunu ve Maker'ın fakültesine ait olduğunu doğrula
        var student = await _context.StudentWorkers
            .Include(sw => sw.Faculty)
            .FirstOrDefaultAsync(sw => sw.Id == dto.StudentWorkerId && sw.IsActive)
            ?? throw new InvalidOperationException($"ID={dto.StudentWorkerId} olan aktif öğrenci bulunamadı.");

        // Multi-Tenant yetki kontrolü: Sekreter yalnızca kendi fakültesinin öğrencisi için işlem yapabilir
        if (makerUser.FacultyId.HasValue && makerUser.FacultyId.Value != student.FacultyId)
            throw new UnauthorizedAccessException(
                $"Bu öğrenci ({student.StudentNumber}) farklı bir fakülteye aittir. Yalnızca kendi fakültenizin öğrencileri için işlem yapabilirsiniz.");

        // 3. Mükerrer kayıt kontrolü: aynı öğrenci + ay + yıl kombinasyonu
        var existingTimesheet = await _context.MonthlyTimesheets
            .AnyAsync(mt =>
                mt.StudentWorkerId == dto.StudentWorkerId &&
                mt.Year == dto.Year &&
                mt.Month == dto.Month);

        if (existingTimesheet)
            throw new InvalidOperationException(
                $"{student.FirstName} {student.LastName} için {dto.Year}/{dto.Month:D2} dönemi puantajı zaten mevcut.");

        // 4. Yeni puantaj kaydını oluştur — Status OTOMATİK olarak PendingSksApproval atanır
        var timesheet = new MonthlyTimesheet
        {
            StudentWorkerId = dto.StudentWorkerId,
            Year            = dto.Year,
            Month           = dto.Month,
            TotalHoursWorked = dto.TotalHoursWorked,
            MakerNote       = dto.MakerNote,
            Status          = TimesheetStatus.PendingSksApproval, // ← Maker statüyü değiştiremez
            CreatedAt       = DateTime.UtcNow
        };

        await _context.MonthlyTimesheets.AddAsync(timesheet);
        await _context.SaveChangesAsync();

        return MapToResultDto(timesheet, student);
    }

    // ─── Checker (SKS Admin) Operasyonları ───────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// İş Kuralları:
    /// 1. Yalnızca <c>PendingSksApproval</c> statüsündeki puantajlar onaylanabilir/reddedilebilir.
    /// 2. Onaylanırsa: Status → <c>ApprovedBySks</c>, ApprovedAt ve ApprovedByUserId dolar.
    /// 3. Reddedilirse: Status → <c>PendingSksApproval</c>'a döner (Maker tekrar düzenleyebilir).
    /// 4. Onay sonrası IskurBotService fire-and-forget olarak tetiklenir.
    /// </remarks>
    public async Task<TimesheetResultDto> ApproveOrRejectTimesheetAsync(TimesheetApproveDto dto, int checkerUserId)
    {
        // 1. Checker kullanıcısını doğrula
        var checkerUser = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == checkerUserId && u.IsActive)
            ?? throw new InvalidOperationException($"ID={checkerUserId} olan aktif kullanıcı bulunamadı.");

        // 2. Puantaj kaydını getir
        var timesheet = await _context.MonthlyTimesheets
            .Include(mt => mt.StudentWorker)
                .ThenInclude(sw => sw.Faculty)
            .FirstOrDefaultAsync(mt => mt.Id == dto.TimesheetId)
            ?? throw new InvalidOperationException($"ID={dto.TimesheetId} olan puantaj kaydı bulunamadı.");

        // 3. Statü kontrolü: Sadece bekleyen kayıtlar işlenebilir
        if (timesheet.Status != TimesheetStatus.PendingSksApproval)
            throw new InvalidOperationException(
                $"Bu puantaj şu an '{timesheet.Status}' statüsündedir ve onay/red işlemi uygulanamaz. " +
                $"Yalnızca '{nameof(TimesheetStatus.PendingSksApproval)}' statüsündeki kayıtlar işlenebilir.");

        if (dto.IsApproved)
        {
            // 4a. ONAY: Status → ApprovedBySks
            timesheet.Status          = TimesheetStatus.ApprovedBySks;
            timesheet.ApprovedAt      = DateTime.UtcNow;
            timesheet.ApprovedByUserId = checkerUserId;
            timesheet.CheckerNote     = dto.CheckerNote;

            _context.MonthlyTimesheets.Update(timesheet);
            await _context.SaveChangesAsync();

            // Playwright otomasyonunu tetikle — fire-and-forget (HTTP yanıtını bloklamaz)
            // Playwright otomasyonunu tetikle — fire-and-forget (HTTP yanıtını bloklamaz)
            // Yeni bir scope oluşturarak, servisin HTTP isteğiyle birlikte ölmesini engelliyoruz.
            int tsId = timesheet.Id;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var botService = scope.ServiceProvider.GetRequiredService<IIskurBotService>();
                await botService.RunTimesheetEntryAsync(tsId, CancellationToken.None);
            }, CancellationToken.None);
        }
        else
        {
            // 4b. RED: Status PendingSksApproval'a döner; Maker tekrar düzenleyebilir
            timesheet.Status      = TimesheetStatus.PendingSksApproval;
            timesheet.CheckerNote = dto.CheckerNote;

            _context.MonthlyTimesheets.Update(timesheet);
            await _context.SaveChangesAsync();
        }

        return MapToResultDto(timesheet, timesheet.StudentWorker);
    }

    // ─── Sorgulama Operasyonları ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TimesheetResultDto?> GetTimesheetByIdAsync(int timesheetId)
    {
        var timesheet = await _context.MonthlyTimesheets
            .Include(mt => mt.StudentWorker)
                .ThenInclude(sw => sw.Faculty)
            .Include(mt => mt.ApprovedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(mt => mt.Id == timesheetId);

        return timesheet is null ? null : MapToResultDto(timesheet, timesheet.StudentWorker);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TimesheetResultDto>> GetTimesheetsByFacultyAsync(int facultyId)
    {
        var timesheets = await _context.MonthlyTimesheets
            .Include(mt => mt.StudentWorker)
                .ThenInclude(sw => sw.Faculty)
            .Include(mt => mt.ApprovedByUser)
            .AsNoTracking()
            .Where(mt => mt.StudentWorker.FacultyId == facultyId)
            .OrderByDescending(mt => mt.Year)
            .ThenByDescending(mt => mt.Month)
            .ToListAsync();

        return timesheets.Select(mt => MapToResultDto(mt, mt.StudentWorker));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TimesheetResultDto>> GetPendingTimesheetsAsync()
    {
        var timesheets = await _context.MonthlyTimesheets
            .Include(mt => mt.StudentWorker)
                .ThenInclude(sw => sw.Faculty)
            .AsNoTracking()
            .Where(mt => mt.Status == TimesheetStatus.PendingSksApproval)
            .OrderBy(mt => mt.CreatedAt)
            .ToListAsync();

        return timesheets.Select(mt => MapToResultDto(mt, mt.StudentWorker));
    }

    // ─── Yardımcı Metodlar ────────────────────────────────────────────────────

    /// <summary>
    /// Entity → DTO dönüşümünü merkezi bir yerden yönetir.
    /// Navigation property'leri flatten ederek API katmanına temiz bir model sunar.
    /// </summary>
    private static TimesheetResultDto MapToResultDto(MonthlyTimesheet timesheet, StudentWorker student)
        => new()
        {
            Id                  = timesheet.Id,
            StudentWorkerId     = timesheet.StudentWorkerId,
            StudentFullName     = $"{student.FirstName} {student.LastName}",
            StudentNationalId   = student.NationalId,
            StudentNumber       = student.StudentNumber,
            FacultyName         = student.Faculty?.Name ?? string.Empty,
            Year                = timesheet.Year,
            Month               = timesheet.Month,
            TotalHoursWorked    = timesheet.TotalHoursWorked,
            Status              = timesheet.Status,
            MakerNote           = timesheet.MakerNote,
            CheckerNote         = timesheet.CheckerNote,
            CreatedAt           = timesheet.CreatedAt,
            ApprovedAt          = timesheet.ApprovedAt,
            ProcessedAt         = timesheet.ProcessedAt,
            ApprovedByUserName  = timesheet.ApprovedByUser?.FullName
        };
}
