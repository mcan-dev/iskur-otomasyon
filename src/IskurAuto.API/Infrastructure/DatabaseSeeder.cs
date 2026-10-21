using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using IskurAuto.Service.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.API.Infrastructure;

/// <summary>
/// Başlangıç verisi. API projesinde durur çünkü parola özetleme (Service) ile veritabanı (Data)
/// bilgisini bir araya getirir; bu iki katmanı birleştirebilen tek yer "composition root"tur.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>Yalnızca geliştirme ortamı için herkesçe bilinen demo hesap parolası. ÜRETİMDE ÇALIŞTIRILMAZ.</summary>
    public const string DemoPassword = "Demo1234!";

    /// <summary>
    /// Demo verisi: bölümler, birimler, her rolden bir kullanıcı ve iki öğrenci.
    /// İkinci öğrenci başka bir bölümde olduğundan "bölüm başkanı yalnızca kendi bölümünü görür"
    /// kuralı arayüzde elle denenebilir. Tüm TC/IBAN değerleri algoritmayla GEÇERLİ ama uydurmadır.
    /// </summary>
    public static async Task SeedDemoDataAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IskurAutoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        if (await db.Departments.AnyAsync()) return;   // veri varsa dokunma

        var bm = new Department { Name = "Bilgisayar Mühendisliği", Code = "BM" };
        var ym = new Department { Name = "Yazılım Mühendisliği", Code = "YM" };
        var eem = new Department { Name = "Elektrik-Elektronik Mühendisliği", Code = "EEM" };
        db.Departments.AddRange(bm, ym, eem);

        db.Holidays.AddRange(
            new Holiday { Date = new DateOnly(2026, 10, 29), Name = "Cumhuriyet Bayramı" },
            new Holiday { Date = new DateOnly(2027, 1, 1), Name = "Yılbaşı" },
            new Holiday { Date = new DateOnly(2027, 4, 23), Name = "Ulusal Egemenlik ve Çocuk Bayramı" },
            new Holiday { Date = new DateOnly(2027, 5, 1), Name = "Emek ve Dayanışma Günü" },
            new Holiday { Date = new DateOnly(2027, 5, 19), Name = "Atatürk'ü Anma, Gençlik ve Spor Bayramı" });

        var dekanlik = new WorkUnit { Name = "Dekanlık", Description = "Dekanlık ofisi" };
        var sekreterlik = new WorkUnit { Name = "Fakülte Sekreterliği" };
        var lab = new WorkUnit { Name = "Bilgisayar Laboratuvarı" };
        var kutuphane = new WorkUnit { Name = "Kütüphane" };
        db.WorkUnits.AddRange(dekanlik, sekreterlik, lab, kutuphane);

        // Her kullanıcı için aynı demo parolası; her seferinde farklı salt ile ayrı özetlenir.
        string Hash() => hasher.Hash(DemoPassword);

        var admin = new User { FullName = "Ayşe Kaya", Email = "sekreter@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.FacultyAdmin };
        var head = new User { FullName = "Prof. Dr. Mehmet Demir", Email = "baskan.bm@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.DepartmentHead, Department = bm };
        var advisor = new User { FullName = "Dr. Öğr. Üyesi Zeynep Arslan", Email = "danisman.bm@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = bm };
        db.Users.AddRange(admin, head, advisor);

        var today = clock.Today;
        var start = today.AddDays(-60);

        var mehmet = NewStudent("Mehmet Öztürk", "ogrenci@teknoloji.edu.tr", "05321234567",
            "12345678950", "TR400006200000000000000001", "2023000001", bm, 2023, start, Hash());
        var elif = NewStudent("Elif Yıldız", "ogrenci2@teknoloji.edu.tr", "05329876543",
            "23456789138", "TR130006200000000000000002", "2024000002", ym, 2024, start, Hash());
        db.StudentProfiles.AddRange(mehmet, elif);

        // Mehmet'e birim ve danışman ata (geçmiş kaydı da böylece görünür olur).
        mehmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = lab, StartDate = start, AssignedByUser = admin });
        mehmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisor, StartDate = start, AssignedByUser = head });

        await db.SaveChangesAsync();

        logger.LogWarning(
            "DEMO VERİSİ oluşturuldu. Tüm demo hesapların parolası '{Password}'. Bu yalnızca geliştirme içindir. " +
            "Hesaplar: sekreter@/baskan.bm@/danisman.bm@/ogrenci@/ogrenci2@teknoloji.edu.tr", DemoPassword);
    }

    /// <summary>
    /// Üretimde ilk yönetici hesabını yapılandırmadan (user-secrets / ortam değişkeni) oluşturur:
    /// Bootstrap:AdminEmail ve Bootstrap:AdminPassword. Sistemde hiç FacultyAdmin yoksa çalışır.
    /// Neden: herkese açık kayıt yalnızca öğrenciler içindir; ilk yönetici başka türlü oluşturulamaz.
    /// </summary>
    public static async Task EnsureBootstrapAdminAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var email = config["Bootstrap:AdminEmail"];
        var password = config["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IskurAutoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Users.AnyAsync(u => u.Role == UserRole.FacultyAdmin)) return;

        db.Users.Add(new User
        {
            FullName = "Sistem Yöneticisi",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = hasher.Hash(password),
            Role = UserRole.FacultyAdmin
        });
        await db.SaveChangesAsync();
        logger.LogWarning("İlk yönetici hesabı oluşturuldu: {Email}. Giriş yaptıktan sonra parolayı değiştirin.", email);
    }

    private static StudentProfile NewStudent(
        string fullName, string email, string phone, string nationalId, string iban, string studentNumber,
        Department department, int registrationYear, DateOnly start, string passwordHash)
    {
        var profile = new StudentProfile
        {
            User = new User { FullName = fullName, Email = email, PhoneNumber = phone, PasswordHash = passwordHash, Role = UserRole.Student },
            NationalId = nationalId,
            Iban = iban,
            StudentNumber = studentNumber,
            Department = department,
            RegistrationYear = registrationYear
        };
        profile.Participations.Add(new ProgramParticipation { StartDate = start });
        return profile;
    }
}
