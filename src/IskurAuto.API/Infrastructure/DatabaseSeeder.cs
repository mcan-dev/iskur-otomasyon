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
        var oldAdvisor = new User { FullName = "Dr. Öğr. Üyesi Hakan Polat", Email = "danisman2.bm@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = bm };
        var advisorYm = new User { FullName = "Dr. Öğr. Üyesi Can Aksoy", Email = "danisman.ym@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = ym };
        // Onay bekleyen başvuru örneği: pasif hesap, fakülte sekreterliği aktif edene kadar giriş yapamaz.
        var pending = new User { FullName = "Dr. Öğr. Üyesi Deniz Kara", Email = "bekleyen.danisman@teknoloji.edu.tr", PhoneNumber = "05361112233", PasswordHash = Hash(), Role = UserRole.Advisor, Department = eem, IsActive = false };
        db.Users.AddRange(admin, head, advisor, oldAdvisor, advisorYm, pending);

        var start = clock.Today.AddDays(-60);

        // Mehmet 2025 Eylül'den beri programda: önceki yıl ve aylara ait kayıtları da olsun.
        var mehmet = NewStudent("Mehmet Öztürk", "ogrenci@teknoloji.edu.tr", "05321234567",
            "12345678950", "TR400006200000000000000001", "2023000001", bm, 2023, new DateOnly(2025, 9, 1), Hash());
        var elif = NewStudent("Elif Yıldız", "ogrenci2@teknoloji.edu.tr", "05329876543",
            "23456789138", "TR130006200000000000000002", "2024000002", ym, 2024, start, Hash());
        db.StudentProfiles.AddRange(mehmet, elif);

        // Mehmet'in birim ve danışman geçmişi: eski satırlar kapalı, güncel olan açık.
        mehmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = kutuphane, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 2, 28), AssignedByUser = admin });
        mehmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = lab, StartDate = new DateOnly(2026, 3, 1), AssignedByUser = admin });
        mehmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = oldAdvisor, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 2, 28), AssignedByUser = head });
        mehmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisor, StartDate = new DateOnly(2026, 3, 1), AssignedByUser = head });

        elif.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = kutuphane, StartDate = start, AssignedByUser = admin });
        elif.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisorYm, StartDate = start, AssignedByUser = admin });

        SeedHistory(db, admin, advisor, mehmet, elif, bm, ym, eem, dekanlik, hashFactory: Hash);

        await db.SaveChangesAsync();

        logger.LogWarning(
            "DEMO VERİSİ oluşturuldu. Tüm demo hesapların parolası '{Password}'. Bu yalnızca geliştirme içindir. " +
            "Hesaplar: sekreter@/baskan.bm@/danisman.bm@/ogrenci@/ogrenci2@teknoloji.edu.tr", DemoPassword);
    }

    /// <summary>
    /// Örnek geçmiş: aynı öğrencinin farklı ay ve yıllardaki planları, izin, devam, görev ve ilişik kesme kaydı.
    /// Vize gösteriminde "aylar ve yıllar birbirinden ayrı kayıtlardır" görülebilsin diye eklenir.
    /// </summary>
    private static void SeedHistory(IskurAutoDbContext db, User admin, User advisor, StudentProfile mehmet, StudentProfile elif,
        Department bm, Department ym, Department eem, WorkUnit dekanlik, Func<string> hashFactory)
    {
        static DateOnly D(int y, int m, int d) => new(y, m, d);

        // Bu ekler fakülte genelindeki filtreleri (bölüm, danışman, durum) denemek içindir.
        var burak = NewStudent("Burak Şahin", "burak.sahin@teknoloji.edu.tr", "05331112233",
            "10000000146", "TR560006200000000000000003", "2025000003", bm, 2025, D(2026, 2, 1), hashFactory());
        var selin = NewStudent("Selin Çelik", "selin.celik@teknoloji.edu.tr", "05344445566",
            "11111111110", "TR780006200000000000000004", "2026000004", eem, 2026, D(2026, 9, 15), hashFactory());
        var kerem = NewStudent("Kerem Aydın", "kerem.aydin@teknoloji.edu.tr", "05355556677",
            "10000000278", "TR900006200000000000000005", "2023000005", bm, 2023, D(2025, 9, 1), hashFactory());
        db.StudentProfiles.AddRange(burak, selin, kerem);

        burak.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = dekanlik, StartDate = D(2026, 2, 1), AssignedByUser = admin });
        burak.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisor, StartDate = D(2026, 2, 1), AssignedByUser = admin });

        // Programdan ayrılan öğrenci: kayıtları silinmez, katılım dönemi kapanır.
        var keremPart = kerem.Participations.First();
        keremPart.EndDate = D(2026, 6, 30);
        keremPart.EndReason = "İlişik kesme: devamsızlık sınırının aşılması";
        db.TerminationDecisions.Add(new TerminationDecision
        {
            StudentProfile = kerem, Basis = TerminationBasis.AbsenceLimitExceeded, DecisionDate = D(2026, 6, 30),
            Note = "Devamsızlık sınırı aşıldığı için ilişik kesildi.", LeaveDaysUsed = 0, UnexcusedAbsenceDays = 5,
            DecidedByUser = admin, CreatedAt = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc)
        });
        foreach (var day in new[] { D(2026, 3, 10), D(2026, 3, 17), D(2026, 3, 24), D(2026, 4, 7), D(2026, 4, 14) })
            db.Absences.Add(new Absence { StudentProfile = kerem, Date = day, Type = AbsenceType.Unexcused, RecordedByUser = admin });

        // Mehmet'in dört ayrı planı: iki farklı yıl, iki farklı ay.
        MonthlyWorkPlan Plan(StudentProfile s, int year, int month, WorkPlanStatus status, string? note, string? review, params int[] days)
        {
            var plan = new MonthlyWorkPlan
            {
                StudentProfile = s, Year = year, Month = month, Status = status, StudentNote = note,
                LastSavedAt = new DateTime(year, month, 1, 7, 30, 0, DateTimeKind.Utc),
                ReviewNote = review
            };
            if (status != WorkPlanStatus.Pending)
            {
                plan.ReviewedByUser = admin;
                plan.ReviewedAt = new DateTime(year, month, 3, 9, 0, 0, DateTimeKind.Utc);
            }
            foreach (var d in days) plan.Days.Add(new WorkPlanDay { Date = D(year, month, d) });
            db.WorkPlans.Add(plan);
            return plan;
        }

        Plan(mehmet, 2025, 9, WorkPlanStatus.Approved, null, null, 8, 10, 15, 17, 22, 24);
        Plan(mehmet, 2025, 10, WorkPlanStatus.Approved, null, null, 6, 8, 13, 15, 20, 22, 27);
        Plan(mehmet, 2026, 9, WorkPlanStatus.Approved, null, null, 7, 9, 14, 16, 21, 23);
        var mehmetOct = Plan(mehmet, 2026, 10, WorkPlanStatus.Approved, "Salı ve perşembe günleri dersim var.", null, 5, 7, 12, 14, 19, 21, 26, 28);
        var elifOct = Plan(elif, 2026, 10, WorkPlanStatus.Pending, "Onay bekliyor.", null, 5, 7, 12, 14, 19, 21);
        Plan(burak, 2026, 10, WorkPlanStatus.Rejected, null, "Haftada en fazla 3 gün seçilebilir, planı yeniden düzenleyin.", 5, 6, 7, 8, 9);

        db.StudentCourseDays.AddRange(
            new StudentCourseDay { StudentProfile = mehmet, DayOfWeek = DayOfWeek.Tuesday },
            new StudentCourseDay { StudentProfile = mehmet, DayOfWeek = DayOfWeek.Thursday });

        // İzinler: günlük onaylı, saatlik onaylı (4 saat = 0,5 gün), onay bekleyen sağlık izni.
        db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                StudentProfile = mehmet, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 10, 12), EndDate = D(2026, 10, 12), DaysCounted = 1, Reason = "Ailevi mazeret",
                AppliedOn = D(2026, 10, 8), ReviewedByUser = admin, ReviewedAt = new DateTime(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc),
                ReviewNote = "Uygun görüldü."
            },
            new LeaveRequest
            {
                StudentProfile = mehmet, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 9, 23), EndDate = D(2026, 9, 23), Hours = 4, DaysCounted = 0.5m, Reason = "Resmî işlem",
                AppliedOn = D(2026, 9, 21), ReviewedByUser = admin, ReviewedAt = new DateTime(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc)
            },
            new LeaveRequest
            {
                StudentProfile = elif, Type = LeaveType.Health, Status = LeaveStatus.Pending,
                StartDate = D(2026, 10, 26), EndDate = D(2026, 10, 27), DaysCounted = 2, Reason = "Hastalık",
                Description = "Doktor raporu sonradan yüklenecek.", AppliedOn = D(2026, 10, 21)
            });

        db.Absences.Add(new Absence { StudentProfile = burak, Date = D(2026, 10, 14), Type = AbsenceType.Unexcused, Note = "Gelmedi, haber vermedi.", RecordedByUser = admin });
        db.AbsenceNotices.Add(new AbsenceNotice
        {
            StudentProfile = burak, Date = D(2026, 10, 14), Message = "Öğrenci bugün çalışma planına rağmen gelmedi.",
            AdvisorUser = advisor, CreatedAt = new DateTime(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc)
        });

        // Devam: Ekim 2026'da dört oturum; Mehmet 12'sinde izinli olduğundan kaydı yok.
        var sessions = new Dictionary<int, AttendanceSession>();
        foreach (var d in new[] { 12, 14, 19, 21 })
            sessions[d] = new AttendanceSession
            {
                Date = D(2026, 10, d), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0),
                Title = "Günlük devam oturumu", SecretKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)),
                CreatedByUser = admin
            };
        db.AttendanceSessions.AddRange(sessions.Values);

        void CheckIn(StudentProfile s, MonthlyWorkPlan plan, int day) => db.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentProfile = s, AttendanceSession = sessions[day], Date = D(2026, 10, day),
            CheckedInAt = new DateTime(2026, 10, day, 6, 12, 0, DateTimeKind.Utc),
            WorkPlanDay = plan.Days.First(x => x.Date == D(2026, 10, day))
        });
        CheckIn(mehmet, mehmetOct, 14); CheckIn(mehmet, mehmetOct, 19); CheckIn(mehmet, mehmetOct, 21);
        CheckIn(elif, elifOct, 19); CheckIn(elif, elifOct, 21);

        // Görev ve değerlendirme: tamamlanmış bir görev (notuyla) ve açık bir görev.
        var done = new StudentTask
        {
            StudentProfile = mehmet, Title = "Laboratuvar envanter sayımı",
            Description = "Bilgisayar laboratuvarındaki cihazların sayımı ve listesi.",
            AssignedDate = D(2026, 10, 5), DueDate = D(2026, 10, 16), AssignedByUser = advisor,
            Status = StudentTaskStatus.Completed, StatusChangedAt = new DateTime(2026, 10, 15, 16, 0, 0, DateTimeKind.Utc)
        };
        done.Evaluations.Add(new TaskEvaluation
        {
            AuthorUser = advisor, Note = "Zamanında ve eksiksiz teslim edildi.", NewStatus = StudentTaskStatus.Completed,
            CreatedAt = new DateTime(2026, 10, 15, 16, 0, 0, DateTimeKind.Utc)
        });
        db.StudentTasks.AddRange(done, new StudentTask
        {
            StudentProfile = mehmet, Title = "Dönem sonu raporu hazırlığı", Description = "Dönem boyunca yapılan işlerin kısa özeti.",
            AssignedDate = D(2026, 10, 19), DueDate = D(2026, 11, 5), AssignedByUser = advisor
        });

        db.ExamPeriods.Add(new ExamPeriod { Name = "2026-2027 Güz dönemi ara sınavları", StartDate = D(2026, 11, 2), EndDate = D(2026, 11, 13) });
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
