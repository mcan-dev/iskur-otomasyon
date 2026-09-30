using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace IskurAuto.Automation;

/// <summary>
/// İŞKUR E-Şube sistemine Playwright botu aracılığıyla otonom puantaj girişi yapan
/// servisin uygulaması.
/// <para>
/// Yalnızca SKS Admin onayından (<c>ApprovedBySks</c>) sonra tetiklenir.
/// Puantaj statüsü ve her otomasyon adımının logu doğrudan bu servis tarafından
/// veritabanına yazılır.
/// </para>
/// </summary>
public class IskurBotService : IIskurBotService
{
    private const string IskurESubeUrl = "https://esube.iskur.gov.tr";

    private readonly IskurAutoDbContext _context;
    private readonly ILogger<IskurBotService> _logger;

    public IskurBotService(IskurAutoDbContext context, ILogger<IskurBotService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task RunTimesheetEntryAsync(
        int timesheetId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Puantaj kaydını veritabanından taze olarak yükle ──────────────
        var timesheet = await _context.MonthlyTimesheets
            .Include(mt => mt.StudentWorker)
                .ThenInclude(sw => sw.Faculty)
            .FirstOrDefaultAsync(mt => mt.Id == timesheetId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"ID={timesheetId} olan puantaj kaydı veritabanında bulunamadı.");

        var student    = timesheet.StudentWorker;
        var studentLog = $"{student.FirstName} {student.LastName} " +
                         $"(TC: {student.NationalId} | ÖğrNo: {student.StudentNumber})";
        var periodLog  = $"{timesheet.Year}/{timesheet.Month:D2}";

        _logger.LogInformation(
            "[BOT] ▶ Başlatıldı — TimesheetId={Id}, Öğrenci={Student}, Dönem={Period}, Saat={Hours}",
            timesheetId, studentLog, periodLog, timesheet.TotalHoursWorked);

        // ── 2. Statü kontrolü: Yalnızca ApprovedBySks işlenebilir ───────────
        if (timesheet.Status != TimesheetStatus.ApprovedBySks)
        {
            var msg = $"Puantaj statüsü işlenemez: '{timesheet.Status}'. " +
                      $"Yalnızca '{nameof(TimesheetStatus.ApprovedBySks)}' statüsündeki kayıtlar işlenir.";
            _logger.LogWarning("[BOT] {Msg}", msg);
            await PersistLogAsync(timesheetId, msg, isSuccess: false,
                errorDetail: "Geçersiz statü", cancellationToken: cancellationToken);
            return;
        }

        // ── 3. Playwright başlat ──────────────────────────────────────────────
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,   // Görünür mod — izleme ve hata ayıklama için
            SlowMo   = 300,     // Her adım arası 300ms bekleme (akışı izlemek için)
            Args     = new[] { "--start-maximized" }
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            Locale       = "tr-TR"
        });

        var page = await context.NewPageAsync();

        try
        {
            // ── ADIM 1: İŞKUR E-Şube giriş sayfasına git ────────────────────
            _logger.LogInformation("[BOT] ADIM 1 — {Url} adresine gidiliyor...", IskurESubeUrl);

            await page.GotoAsync(IskurESubeUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout   = 30_000
            });

            var pageTitle = await page.TitleAsync();
            _logger.LogInformation("[BOT] ADIM 1 ✓ — Sayfa yüklendi. Başlık: '{Title}'", pageTitle);
            await PersistLogAsync(timesheetId,
                $"ADIM 1: İŞKUR E-Şube ({IskurESubeUrl}) sayfası başarıyla yüklendi. Başlık: '{pageTitle}'",
                isSuccess: true, cancellationToken: cancellationToken);

            // ── ADIM 2: 5 saniye bekle (giriş simülasyonu) ───────────────────
            _logger.LogInformation("[BOT] ADIM 2 — Giriş simülasyonu: 5 saniye bekleniyor...");
            await PersistLogAsync(timesheetId,
                $"ADIM 2: Giriş simülasyonu — {studentLog} için 5 saniye bekleniyor.",
                isSuccess: true, cancellationToken: cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            _logger.LogInformation("[BOT] ADIM 2 ✓ — Bekleme tamamlandı.");

            // ── ADIM 3: Gerçek otomasyon adımları için yer tutucular ──────────
            // TODO: Login — kullanıcı adı ve şifre alanlarını doldur ve giriş yap
            // TODO: "Kısmi Zamanlı Öğrenci İşlemleri" menüsüne git
            // TODO: İşyeri/fakülte seçimi yap (student.Faculty.Name)
            // TODO: Öğrenciyi TC kimlik no ile ara: student.NationalId
            // TODO: İlgili dönemi seç: timesheet.Year / timesheet.Month
            // TODO: Toplam çalışılan saati gir: timesheet.TotalHoursWorked
            // TODO: Kaydet / Onayla butonuna tıkla
            // TODO: Başarı onay mesajını doğrula (Assert)

            // ── ADIM 4: İşlemi başarılı say ve statüyü güncelle ──────────────
            _logger.LogInformation(
                "[BOT] ADIM 4 — Statü ProcessedToIskur olarak güncelleniyor — TimesheetId={Id}", timesheetId);

            timesheet.Status      = TimesheetStatus.ProcessedToIskur;
            timesheet.ProcessedAt = DateTime.UtcNow;
            _context.MonthlyTimesheets.Update(timesheet);

            await PersistLogAsync(timesheetId,
                $"ADIM 4: Puantaj İŞKUR sistemine başarıyla girildi. " +
                $"Öğrenci: {studentLog}, Dönem: {periodLog}, " +
                $"Toplam Saat: {timesheet.TotalHoursWorked}",
                isSuccess: true, cancellationToken: cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[BOT] ✅ Tamamlandı — TimesheetId={Id}, Statü={Status}",
                timesheetId, timesheet.Status);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[BOT] ⚠ İptal isteği alındı — TimesheetId={Id}", timesheetId);

            await SetErrorStatusAsync(timesheet,
                step:        "İşlem kullanıcı tarafından iptal edildi.",
                errorDetail: "OperationCanceledException",
                cancellationToken: CancellationToken.None); // İptal edilse bile logu yaz
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[BOT] ❌ HATA — TimesheetId={Id}, Öğrenci={Student}",
                timesheetId, studentLog);

            await SetErrorStatusAsync(timesheet,
                step:        $"Beklenmedik hata oluştu: {ex.Message}",
                errorDetail: ex.Message,
                stackTrace:  ex.StackTrace,
                cancellationToken: CancellationToken.None);
            throw;
        }
        finally
        {
            _logger.LogInformation("[BOT] 🔒 Tarayıcı oturumu kapatılıyor — TimesheetId={Id}", timesheetId);
        }
    }

    // ─── Yardımcı: Log Kaydı ─────────────────────────────────────────────────

    /// <summary>
    /// Otomasyon adımını <c>TaskLog</c> tablosuna kaydeder ve hemen kalıcı hale getirir.
    /// Her adım için ayrı SaveChanges çağrısı yapılır; bu sayede hata durumunda
    /// tamamlanan adımların logları kaybolmaz.
    /// </summary>
    private async Task PersistLogAsync(
        int timesheetId,
        string stepDescription,
        bool isSuccess,
        string? errorDetail = null,
        string? stackTrace  = null,
        CancellationToken cancellationToken = default)
    {
        var log = new TaskLog
        {
            MonthlyTimesheetId = timesheetId,
            StepDescription    = stepDescription,
            IsSuccess          = isSuccess,
            ErrorDetail        = errorDetail,
            StackTrace         = stackTrace,
            LoggedAt           = DateTime.UtcNow
        };

        await _context.TaskLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Hata durumunda puantaj statüsünü <c>Error</c> olarak günceller ve log yazar.
    /// </summary>
    private async Task SetErrorStatusAsync(
        MonthlyTimesheet timesheet,
        string step,
        string? errorDetail = null,
        string? stackTrace  = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            timesheet.Status = TimesheetStatus.Error;
            _context.MonthlyTimesheets.Update(timesheet);

            await PersistLogAsync(timesheet.Id,
                stepDescription: step,
                isSuccess:   false,
                errorDetail: errorDetail,
                stackTrace:  stackTrace,
                cancellationToken: cancellationToken);
        }
        catch (Exception dbEx)
        {
            // Hata loglaması bile başarısız olursa sadece loglayıp sessizce geç
            _logger.LogCritical(dbEx,
                "[BOT] Hata statüsü veritabanına yazılamadı — TimesheetId={Id}", timesheet.Id);
        }
    }
}
