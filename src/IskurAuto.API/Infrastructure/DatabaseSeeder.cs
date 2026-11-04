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
        var eem = new Department { Name = "Elektrik-Elektronik Mühendisliği", Code = "EEM" };
        var mak = new Department { Name = "Makine Mühendisliği", Code = "MAK" };
        var mek = new Department { Name = "Mekatronik Mühendisliği", Code = "MEK" };
        var met = new Department { Name = "Metalurji ve Malzeme Mühendisliği", Code = "MET" };
        db.Departments.AddRange(bm, eem, mak, mek, met);

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
        var advisorMak = new User { FullName = "Dr. Öğr. Üyesi Can Aksoy", Email = "danisman.mak@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = mak };
        // Onay bekleyen başvuru örneği: pasif hesap, fakülte sekreterliği aktif edene kadar giriş yapamaz.
        var pending = new User { FullName = "Dr. Öğr. Üyesi Deniz Kara", Email = "bekleyen.danisman@teknoloji.edu.tr", PhoneNumber = "05361112233", PasswordHash = Hash(), Role = UserRole.Advisor, Department = eem, IsActive = false, IsPendingApproval = true };
        var pendingMak = new User { FullName = "Dr. Öğr. Üyesi Seda Korkmaz", Email = "bekleyen.danisman2@teknoloji.edu.tr", PhoneNumber = "05372223344", PasswordHash = Hash(), Role = UserRole.Advisor, Department = mak, IsActive = false, IsPendingApproval = true };
        var advisorEem = new User { FullName = "Dr. Öğr. Üyesi Pınar Dede", Email = "danisman.eem@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = eem };
        var headEem = new User { FullName = "Prof. Dr. Selma Koç", Email = "baskan.eem@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.DepartmentHead, Department = eem };
        var headMak = new User { FullName = "Prof. Dr. Orhan Tekin", Email = "baskan.mak@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.DepartmentHead, Department = mak };
        var headMek = new User { FullName = "Prof. Dr. Nilüfer Aydoğan", Email = "baskan.mek@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.DepartmentHead, Department = mek };
        var headMet = new User { FullName = "Prof. Dr. Kadir Sezer", Email = "baskan.met@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.DepartmentHead, Department = met };
        var advisorMek = new User { FullName = "Dr. Öğr. Üyesi Burcu Er", Email = "danisman.mek@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = mek };
        var advisorMet = new User { FullName = "Dr. Öğr. Üyesi Tolga Yaman", Email = "danisman.met@teknoloji.edu.tr", PasswordHash = Hash(), Role = UserRole.Advisor, Department = met };
        db.Users.AddRange(admin, head, advisor, oldAdvisor, advisorMak, pending, pendingMak, advisorEem, headEem, headMak, headMek, headMet, advisorMek, advisorMet);

        var start = clock.Today.AddDays(-60);

        // Mehmet 2025 Eylül'den beri programda: önceki yıl ve aylara ait kayıtları da olsun.
        var mehmet = NewStudent("Mehmet Öztürk", "ogrenci@teknoloji.edu.tr", "05321234567",
            "12345678950", "TR400006200000000000000001", "230000001", bm, 2023, new DateOnly(2025, 9, 1), Hash());
        var elif = NewStudent("Elif Yıldız", "ogrenci2@teknoloji.edu.tr", "05329876543",
            "23456789138", "TR130006200000000000000002", "240000002", mak, 2024, start, Hash());
        db.StudentProfiles.AddRange(mehmet, elif);

        // Mehmet'in birim ve danışman geçmişi: eski satırlar kapalı, güncel olan açık.
        mehmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = kutuphane, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 2, 28), AssignedByUser = admin });
        mehmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = lab, StartDate = new DateOnly(2026, 3, 1), AssignedByUser = admin });
        mehmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = oldAdvisor, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 2, 28), AssignedByUser = head });
        mehmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisor, StartDate = new DateOnly(2026, 3, 1), AssignedByUser = head });

        elif.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = kutuphane, StartDate = start, AssignedByUser = admin });
        elif.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisorMak, StartDate = start, AssignedByUser = admin });

        SeedHistory(db, admin, advisor, advisorMak, advisorMek, advisorMet, advisorEem, head, headEem, headMak, headMek, headMet, mehmet, elif, bm, eem, mak, mek, met, dekanlik, sekreterlik, lab, kutuphane, clock.Today, hashFactory: Hash);

        await db.SaveChangesAsync();

        logger.LogWarning(
            "DEMO VERİSİ oluşturuldu. Tüm demo hesapların parolası '{Password}'. Bu yalnızca geliştirme içindir. " +
            "Hesaplar: sekreter@/baskan.bm@/danisman.bm@/ogrenci@/ogrenci2@teknoloji.edu.tr", DemoPassword);
    }

    /// <summary>
    /// Örnek geçmiş: aynı öğrencinin farklı ay ve yıllardaki planları, izin, devam, görev ve ilişik kesme kaydı.
    /// Vize gösteriminde "aylar ve yıllar birbirinden ayrı kayıtlardır" görülebilsin diye eklenir.
    /// </summary>
    private static void SeedHistory(IskurAutoDbContext db, User admin, User advisor, User advisorMak, User advisorMek, User advisorMet, User advisorEem, User head,
        User headEem, User headMak, User headMek, User headMet,
        StudentProfile mehmet, StudentProfile elif, Department bm, Department eem, Department mak, Department mek, Department met,
        WorkUnit dekanlik, WorkUnit sekreterlik, WorkUnit lab, WorkUnit kutuphane, DateOnly today, Func<string> hashFactory)
    {
        static DateOnly D(int y, int m, int d) => new(y, m, d);

        // Bu ekler fakülte genelindeki filtreleri (bölüm, danışman, durum) denemek içindir.
        var burak = NewStudent("Burak Şahin", "burak.sahin@teknoloji.edu.tr", "05331112233",
            "10000000146", "TR830006200000000000000003", "250000003", bm, 2025, D(2026, 2, 1), hashFactory());
        var selin = NewStudent("Selin Çelik", "selin.celik@teknoloji.edu.tr", "05344445566",
            "11111111110", "TR560006200000000000000004", "260000004", eem, 2026, D(2026, 9, 15), hashFactory());
        var kerem = NewStudent("Kerem Aydın", "kerem.aydin@teknoloji.edu.tr", "05355556677",
            "10000000214", "TR290006200000000000000005", "230000005", bm, 2023, D(2025, 9, 1), hashFactory());
        // Mekatronik ve Metalurji bölümlerinden de öğrenci bulunsun diye (beş bölümün hepsinde kayıt olur).
        var ahmet = NewStudent("Ahmet Kılıç", "ahmet.kilic@teknoloji.edu.tr", "05366667788",
            "10000000382", "TR020006200000000000000006", "250000006", mek, 2025, D(2026, 2, 2), hashFactory());
        var fatma = NewStudent("Fatma Güneş", "fatma.gunes@teknoloji.edu.tr", "05377778899",
            "10000000450", "TR720006200000000000000007", "240000007", met, 2024, D(2025, 10, 1), hashFactory());
        db.StudentProfiles.AddRange(burak, selin, kerem, ahmet, fatma);
        ahmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = kutuphane, StartDate = D(2026, 2, 2), AssignedByUser = admin });
        ahmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisorMek, StartDate = D(2026, 2, 2), AssignedByUser = admin });
        fatma.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = lab, StartDate = D(2025, 10, 1), AssignedByUser = admin });
        fatma.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisorMet, StartDate = D(2025, 10, 1), AssignedByUser = admin });

        selin.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = sekreterlik, StartDate = D(2026, 9, 15), AssignedByUser = admin });
        selin.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisorEem, StartDate = D(2026, 9, 15), AssignedByUser = headEem });

        burak.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = dekanlik, StartDate = D(2026, 2, 1), AssignedByUser = admin });
        burak.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisor, StartDate = D(2026, 2, 1), AssignedByUser = admin });

        // Programdan ayrılan öğrenci: kayıtları silinmez, katılım dönemi kapanır.
        var keremPart = kerem.Participations.First();
        keremPart.EndDate = D(2026, 6, 30);
        keremPart.EndReason = "İlişik kesme: devamsızlık sınırının aşılması";
        db.TerminationDecisions.Add(new TerminationDecision
        {
            StudentProfile = kerem, Basis = TerminationBasis.Other, DecisionDate = D(2026, 6, 30),
            Note = "Devamsızlık nedeniyle ilişik kesildi.", LeaveDaysUsed = 5, AbsenceDays = 5,
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
        var elifOct = Plan(elif, 2026, 10, WorkPlanStatus.Approved, "Pazartesi öğleden sonra dersim var.", null, 5, 7, 12, 14, 19, 21);
        var ahmetOct = Plan(ahmet, 2026, 10, WorkPlanStatus.Approved, null, null, 5, 7, 12, 14, 19, 21);
        Plan(fatma, 2026, 9, WorkPlanStatus.Approved, null, null, 8, 10, 15, 17, 22, 24);
        var fatmaOct = Plan(fatma, 2026, 10, WorkPlanStatus.Approved, "Salı günleri dersim var.", null, 5, 7, 12, 14);
        Plan(burak, 2026, 10, WorkPlanStatus.Rejected, null, "Haftada en fazla 3 gün seçilebilir, planı yeniden düzenleyin.", 5, 6, 7, 8, 9);

        db.StudentCourseDays.AddRange(
            new StudentCourseDay { StudentProfile = mehmet, DayOfWeek = DayOfWeek.Tuesday, Session = CourseSession.FullDay },
            new StudentCourseDay { StudentProfile = mehmet, DayOfWeek = DayOfWeek.Thursday, Session = CourseSession.Afternoon });

        // İzinler: onaylı gün izinleri ve onay bekleyen sağlık izni.
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
                StartDate = D(2026, 9, 23), EndDate = D(2026, 9, 23), DaysCounted = 1, Reason = "Resmî işlem",
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
        foreach (var d in new[] { 5, 7, 12, 14, 19, 21 })
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
        CheckIn(mehmet, mehmetOct, 7);
        foreach (var day in new[] { 5, 7, 12, 14, 19 }) CheckIn(ahmet, ahmetOct, day);
        CheckIn(fatma, fatmaOct, 5); CheckIn(fatma, fatmaOct, 12);

        SeedExtras(db, admin, advisor, advisorMak, head, mehmet, elif, burak, selin, kerem, ahmet, mehmetOct, elifOct, sessions);

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

        SeedAllRoles(db, admin, advisor, advisorMak, advisorMek, advisorMet, advisorEem, head, headEem, headMak, headMek, headMet,
            mehmet, elif, burak, selin, ahmet, fatma, bm, eem, mak, mek, met, dekanlik, sekreterlik, lab, kutuphane, today, hashFactory);
    }

    /// <summary>
    /// Dört rolün (sekreterlik, bölüm başkanı, danışman, öğrenci) ve beş bölümün hiçbir ekranı boş kalmasın diye
    /// her öğrenciye ders günü, izin, devamsızlık, bildirim, görev ve (Kasım için) plan ekler.
    /// </summary>
    private static void SeedAllRoles(IskurAutoDbContext db, User admin, User advisor, User advisorMak, User advisorMek, User advisorMet,
        User advisorEem, User head, User headEem, User headMak, User headMek, User headMet,
        StudentProfile mehmet, StudentProfile elif, StudentProfile burak, StudentProfile selin, StudentProfile ahmet, StudentProfile fatma,
        Department bm, Department eem, Department mak, Department mek, Department met,
        WorkUnit dekanlik, WorkUnit sekreterlik, WorkUnit lab, WorkUnit kutuphane, DateOnly today, Func<string> hashFactory)
    {
        static DateOnly D(int y, int m, int d) => new(y, m, d);
        static DateTime T(int y, int m, int d, int h, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

        // Rapor ekranları dolu görünsün diye beş bölümden beş öğrenci daha: Ekim ve Kasım'da onaylı plan,
        // karekodla devam kayıtları, izinli ve devamsız günler. (Kimlik ve IBAN değerleri geçerli biçimde ama uydurmadır.)
        var zeynep = NewStudent("Zeynep Acar", "zeynep.acar@teknoloji.edu.tr", "05381110011", "30000001172", "TR450006200000000000000008", "300000011", bm, 2024, D(2025, 10, 1), hashFactory());
        var emre = NewStudent("Emre Demir", "emre.demir@teknoloji.edu.tr", "05382220022", "31000002220", "TR180006200000000000000009", "310000022", eem, 2025, D(2026, 2, 2), hashFactory());
        var berkay = NewStudent("Berkay Sönmez", "berkay.sonmez@teknoloji.edu.tr", "05383330033", "32000003378", "TR880006200000000000000010", "320000033", mak, 2024, D(2025, 10, 1), hashFactory());
        var merve = NewStudent("Merve Tuna", "merve.tuna@teknoloji.edu.tr", "05384440044", "33000004426", "TR610006200000000000000011", "330000044", mek, 2025, D(2026, 2, 2), hashFactory());
        var oguz = NewStudent("Oğuz Kara", "oguz.kara@teknoloji.edu.tr", "05385550055", "34000005574", "TR340006200000000000000012", "340000055", met, 2026, D(2026, 9, 15), hashFactory());
        db.StudentProfiles.AddRange(zeynep, emre, berkay, merve, oguz);

        void Assign(StudentProfile s, User advisorUser, WorkUnit unit, User by, DateOnly since)
        {
            s.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = unit, StartDate = since, AssignedByUser = admin });
            s.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisorUser, StartDate = since, AssignedByUser = by });
        }
        Assign(zeynep, advisor, lab, head, D(2025, 10, 1));
        Assign(emre, advisorEem, kutuphane, headEem, D(2026, 2, 2));
        Assign(berkay, advisorMak, sekreterlik, headMak, D(2025, 10, 1));
        Assign(merve, advisorMek, dekanlik, headMek, D(2026, 2, 2));
        Assign(oguz, advisorMet, lab, headMet, D(2026, 9, 15));

        MonthlyWorkPlan ApprovedPlan(StudentProfile s, int year, int month, params int[] days)
        {
            var plan = new MonthlyWorkPlan
            {
                StudentProfile = s, Year = year, Month = month, Status = WorkPlanStatus.Approved,
                LastSavedAt = T(year, month, 1, 8), ReviewedByUser = admin, ReviewedAt = T(year, month, 2, 9)
            };
            foreach (var d in days) plan.Days.Add(new WorkPlanDay { Date = D(year, month, d) });
            db.WorkPlans.Add(plan);
            return plan;
        }

        // Oturumlar: Ekim'de 5, 7, 12, 14, 19, 21; Kasım'da 2, 3 ve 4 (yoksa eklenir).
        AttendanceSession SessionOn(int month, int day)
        {
            var date = D(2026, month, day);
            var existing = db.AttendanceSessions.Local.FirstOrDefault(x => x.Date == date && x.DepartmentId == null && x.Department == null);
            if (existing is not null) return existing;
            var created = new AttendanceSession
            {
                Date = date, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0), Title = "Günlük devam oturumu",
                SecretKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)), CreatedByUser = admin
            };
            db.AttendanceSessions.Add(created);
            return created;
        }
        void Attend(StudentProfile s, MonthlyWorkPlan plan, int month, params int[] days)
        {
            foreach (var d in days)
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    StudentProfile = s, AttendanceSession = SessionOn(month, d), Date = D(2026, month, d),
                    CheckedInAt = T(2026, month, d, 6, 10 + d % 40), WorkPlanDay = plan.Days.First(x => x.Date == D(2026, month, d))
                });
        }

        var zOct = ApprovedPlan(zeynep, 2026, 10, 5, 7, 12, 14, 19, 21);
        Attend(zeynep, zOct, 10, 5, 7, 12, 14, 19);
        var zNov = ApprovedPlan(zeynep, 2026, 11, 2, 4, 17, 19, 24, 26);
        Attend(zeynep, zNov, 11, 2, 4);

        var eOct = ApprovedPlan(emre, 2026, 10, 5, 7, 12, 14, 19, 21);
        Attend(emre, eOct, 10, 5, 12, 14, 21);
        var eNov = ApprovedPlan(emre, 2026, 11, 3, 4, 10, 12, 17, 19);
        Attend(emre, eNov, 11, 3, 4);

        var bOct = ApprovedPlan(berkay, 2026, 10, 5, 7, 12, 14);
        Attend(berkay, bOct, 10, 5, 7, 12, 14);
        var bNov = ApprovedPlan(berkay, 2026, 11, 2, 3, 9, 11, 16, 18);
        Attend(berkay, bNov, 11, 2, 3);

        var mOct = ApprovedPlan(merve, 2026, 10, 7, 12, 14, 19, 21);
        Attend(merve, mOct, 10, 7, 12, 19);
        var mNov = ApprovedPlan(merve, 2026, 11, 4, 6, 11, 13, 18, 20);
        Attend(merve, mNov, 11, 4);

        var oOct = ApprovedPlan(oguz, 2026, 10, 5, 7, 12, 14, 19, 21);
        Attend(oguz, oOct, 10, 5, 7, 12, 14, 19, 21);
        db.WorkPlans.Add(new MonthlyWorkPlan
        {
            StudentProfile = oguz, Year = 2026, Month = 11, Status = WorkPlanStatus.Pending, LastSavedAt = T(2026, 11, 3, 9),
            Days = { new WorkPlanDay { Date = D(2026, 11, 3) }, new WorkPlanDay { Date = D(2026, 11, 5) }, new WorkPlanDay { Date = D(2026, 11, 17) },
                     new WorkPlanDay { Date = D(2026, 11, 19) }, new WorkPlanDay { Date = D(2026, 11, 24) }, new WorkPlanDay { Date = D(2026, 11, 26) } }
        });

        // Mevcut öğrencilerin Kasım'daki ilk günleri: karekodla devam kaydı.
        var plansByStudent = db.WorkPlans.Local.Where(x => x.Year == 2026 && x.Month == 11 && x.Status == WorkPlanStatus.Approved).ToList();
        MonthlyWorkPlan? NovPlan(StudentProfile s) => plansByStudent.FirstOrDefault(x => x.StudentProfile == s);
        if (NovPlan(mehmet) is { } mehmetNov) Attend(mehmet, mehmetNov, 11, 3);
        if (NovPlan(selin) is { } selinNov) Attend(selin, selinNov, 11, 3);
        if (NovPlan(ahmet) is { } ahmetNov) Attend(ahmet, ahmetNov, 11, 4);
        if (NovPlan(burak) is { } burakNov) Attend(burak, burakNov, 11, 4);

        // İzin ve devamsızlık: zeynep 21 Ekim'de izinli (planlı gün), berkay 9 Kasım için onaylı izin, emre bekleyen izin.
        db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                StudentProfile = zeynep, Type = LeaveType.Health, Status = LeaveStatus.Approved,
                StartDate = D(2026, 10, 21), EndDate = D(2026, 10, 21), DaysCounted = 1, Reason = "Sağlık kontrolü",
                AppliedOn = D(2026, 10, 15), ReviewedByUser = advisor, ReviewedAt = T(2026, 10, 16, 10), ReviewNote = "Uygun görüldü."
            },
            new LeaveRequest
            {
                StudentProfile = berkay, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 11, 9), EndDate = D(2026, 11, 9), DaysCounted = 1, Reason = "Resmî işlem",
                AppliedOn = D(2026, 11, 2), ReviewedByUser = headMak, ReviewedAt = T(2026, 11, 3, 11)
            },
            new LeaveRequest
            {
                StudentProfile = emre, Type = LeaveType.Excuse, Status = LeaveStatus.Pending,
                StartDate = D(2026, 11, 12), EndDate = D(2026, 11, 12), DaysCounted = 1, Reason = "Ailevi mazeret",
                AppliedOn = D(2026, 11, 3)
            },
            new LeaveRequest
            {
                StudentProfile = merve, Type = LeaveType.Other, Status = LeaveStatus.Pending,
                StartDate = D(2026, 11, 18), EndDate = D(2026, 11, 18), DaysCounted = 1, Reason = "Kongre katılımı",
                AppliedOn = D(2026, 11, 4)
            });
        db.Absences.AddRange(
            new Absence { StudentProfile = merve, Date = D(2026, 10, 14), Type = AbsenceType.Unexcused, Note = "Gelmedi, haber vermedi.", RecordedByUser = admin });

        // Ders günleri: takvimde "Dersli gün" olarak görünür.
        db.StudentCourseDays.AddRange(
            new StudentCourseDay { StudentProfile = elif, DayOfWeek = DayOfWeek.Monday, Session = CourseSession.Afternoon },
            new StudentCourseDay { StudentProfile = burak, DayOfWeek = DayOfWeek.Wednesday, Session = CourseSession.FullDay },
            new StudentCourseDay { StudentProfile = selin, DayOfWeek = DayOfWeek.Friday, Session = CourseSession.Morning },
            new StudentCourseDay { StudentProfile = ahmet, DayOfWeek = DayOfWeek.Tuesday, Session = CourseSession.Morning },
            new StudentCourseDay { StudentProfile = fatma, DayOfWeek = DayOfWeek.Tuesday, Session = CourseSession.FullDay });

        // Kasım için onay bekleyen planlar (fakülte "Plan Onayları" ekranı ve ana sayfa sayacı dolu olsun).
        void PendingPlan(StudentProfile s, string? note, params int[] days)
        {
            var plan = new MonthlyWorkPlan
            {
                StudentProfile = s, Year = 2026, Month = 11, Status = WorkPlanStatus.Pending, StudentNote = note,
                LastSavedAt = T(2026, 11, 3, 8)
            };
            foreach (var d in days) plan.Days.Add(new WorkPlanDay { Date = D(2026, 11, d) });
            db.WorkPlans.Add(plan);
        }
        PendingPlan(elif, "Pazartesi öğleden sonra dersim var.", 3, 5, 17, 19, 24, 26);
        PendingPlan(fatma, "Salı günleri dersim var.", 4, 6, 18, 20, 25, 27);

        // İzinler: her bölümde onaylı, bekleyen ve (bazılarında) reddedilmiş talep.
        db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                StudentProfile = ahmet, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 10, 26), EndDate = D(2026, 10, 26), DaysCounted = 1, Reason = "Resmî işlem",
                AppliedOn = D(2026, 10, 20), ReviewedByUser = admin, ReviewedAt = T(2026, 10, 21, 10)
            },
            new LeaveRequest
            {
                StudentProfile = ahmet, Type = LeaveType.Health, Status = LeaveStatus.Pending,
                StartDate = D(2026, 11, 18), EndDate = D(2026, 11, 18), DaysCounted = 1, Reason = "Diş hekimi randevusu",
                AppliedOn = D(2026, 11, 3)
            },
            new LeaveRequest
            {
                StudentProfile = fatma, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 10, 14), EndDate = D(2026, 10, 14), DaysCounted = 1, Reason = "Aile ziyareti",
                AppliedOn = D(2026, 10, 9), ReviewedByUser = admin, ReviewedAt = T(2026, 10, 10, 11)
            },
            new LeaveRequest
            {
                StudentProfile = fatma, Type = LeaveType.Other, Status = LeaveStatus.Rejected,
                StartDate = D(2026, 10, 19), EndDate = D(2026, 10, 20), DaysCounted = 2, Reason = "Kişisel iş",
                AppliedOn = D(2026, 10, 15), ReviewedByUser = admin, ReviewedAt = T(2026, 10, 16, 9),
                ReviewNote = "Dönem içi izin hakkı yerine plan değişikliği önerildi."
            },
            new LeaveRequest
            {
                StudentProfile = elif, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 9, 16), EndDate = D(2026, 9, 16), DaysCounted = 1, Reason = "Sınav hazırlığı",
                AppliedOn = D(2026, 9, 10), ReviewedByUser = admin, ReviewedAt = T(2026, 9, 11, 10)
            },
            new LeaveRequest
            {
                StudentProfile = selin, Type = LeaveType.Excuse, Status = LeaveStatus.Pending,
                StartDate = D(2026, 11, 10), EndDate = D(2026, 11, 10), DaysCounted = 1, Reason = "Kişisel işler",
                AppliedOn = D(2026, 11, 3)
            },
            new LeaveRequest
            {
                StudentProfile = burak, Type = LeaveType.Health, Status = LeaveStatus.Pending,
                StartDate = D(2026, 11, 12), EndDate = D(2026, 11, 12), DaysCounted = 1, Reason = "Kontrol muayenesi",
                AppliedOn = D(2026, 11, 2)
            });

        // Devamsızlıklar (planlı günde devam kaydı bulunmayan günler) ve her danışmandan bölüm başkanına bildirim.
        db.Absences.AddRange(
            new Absence { StudentProfile = ahmet, Date = D(2026, 10, 21), Type = AbsenceType.Unexcused, Note = "Yoklamada görünmedi.", RecordedByUser = admin },
            new Absence { StudentProfile = fatma, Date = D(2026, 10, 7), Type = AbsenceType.HealthReport, Note = "Sağlık raporu sonradan getirildi.", RecordedByUser = admin },
            new Absence { StudentProfile = elif, Date = D(2026, 10, 7), Type = AbsenceType.Unexcused, Note = "Çalışma yerine gelmedi.", RecordedByUser = admin });

        db.AbsenceNotices.AddRange(
            new AbsenceNotice { StudentProfile = elif, Date = D(2026, 10, 7), Message = "Öğrenci 7 Ekim'de çalışma yerine gelmedi.", AdvisorUser = advisorMak, CreatedAt = T(2026, 10, 7, 14) },
            new AbsenceNotice { StudentProfile = ahmet, Date = D(2026, 10, 21), Message = "Öğrenci 21 Ekim'de planlı olduğu halde görünmedi.", AdvisorUser = advisorMek, CreatedAt = T(2026, 10, 21, 15) },
            new AbsenceNotice { StudentProfile = fatma, Date = D(2026, 10, 7), Message = "Öğrenci rahatsızlığı nedeniyle gelemedi, rapor bekleniyor.", AdvisorUser = advisorMet, CreatedAt = T(2026, 10, 7, 13), ReadAt = T(2026, 10, 8, 9), ReadByUser = headMet },
            new AbsenceNotice { StudentProfile = selin, Date = D(2026, 10, 15), Message = "Öğrenci üst üste devamsızlık yaptı, izin hakkını aştı.", AdvisorUser = advisorEem, CreatedAt = T(2026, 10, 15, 16) });

        // Görevler: her bölümde en az bir görev; tamamlanmış olanlarda değerlendirme notu da vardır.
        StudentTask Task(StudentProfile s, User by, string title, string description, DateOnly given, DateOnly due, StudentTaskStatus status, string? note)
        {
            var t = new StudentTask
            {
                StudentProfile = s, Title = title, Description = description, AssignedDate = given, DueDate = due, AssignedByUser = by,
                Status = status
            };
            if (status != StudentTaskStatus.Open)
            {
                t.StatusChangedAt = T(due.Year, due.Month, due.Day, 15);
                t.Evaluations.Add(new TaskEvaluation { AuthorUser = by, Note = note ?? "Değerlendirildi.", NewStatus = status, CreatedAt = T(due.Year, due.Month, due.Day, 15) });
            }
            return t;
        }
        db.StudentTasks.AddRange(
            Task(selin, advisorEem, "Elektrik panosu envanteri", "Laboratuvardaki pano ve kablo malzemelerinin sayımı.", D(2026, 10, 19), D(2026, 11, 13), StudentTaskStatus.Open, null),
            Task(ahmet, advisorMek, "Mekatronik atölyesi bakım listesi", "Atölyedeki cihazların bakım takvimini çıkarma.", D(2026, 10, 5), D(2026, 10, 19), StudentTaskStatus.Completed, "Liste düzenli ve eksiksiz hazırlandı."),
            Task(ahmet, advisorMek, "Sensör deneyleri raporu", "Deney sonuçlarının kısa raporu.", D(2026, 10, 26), D(2026, 11, 12), StudentTaskStatus.Open, null),
            Task(fatma, advisorMet, "Numune etiketleme", "Metalografi numunelerinin etiketlenip dosyalanması.", D(2026, 10, 6), D(2026, 10, 20), StudentTaskStatus.Completed, "Etiketler doğru ve okunaklı."),
            Task(fatma, advisorMet, "Malzeme katalog taraması", "Tedarikçi kataloglarından fiyat listesi çıkarılması.", D(2026, 10, 21), D(2026, 11, 6), StudentTaskStatus.Open, null),
            Task(elif, advisorMak, "Atölye güvenlik formları", "Atölye girişindeki güvenlik formlarının güncellenmesi.", D(2026, 9, 14), D(2026, 9, 28), StudentTaskStatus.Completed, "Zamanında teslim edildi."),
            Task(burak, advisor, "Bilgisayar envanter kaydı", "Laboratuvar bilgisayarlarının envanter kaydı.", D(2026, 10, 27), D(2026, 11, 13), StudentTaskStatus.Open, null));

        // Karekod oturumları: bugünün oturumları (fakülte geneli ve BM bölümü) ile 3 Kasım'daki oturum.
        string Key() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        if (!db.AttendanceSessions.Local.Any(x => x.Date == today && x.Department == null))
            db.AttendanceSessions.Add(new AttendanceSession { Date = today, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(17, 30), Title = "Bugünkü devam oturumu (fakülte geneli)", SecretKey = Key(), CreatedByUser = admin });
        db.AttendanceSessions.AddRange(
            new AttendanceSession { Date = today, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(16, 0), Title = "Bilgisayar Mühendisliği devam oturumu", SecretKey = Key(), CreatedByUser = head, Department = bm });

        // Kayıt ve onay başvurularının izi.
        void Log(DateTime at, string email, string action, string entity, string? details)
            => db.AuditLogs.Add(new AuditLog { OccurredAt = at, UserId = null, UserEmail = email, Action = action, EntityName = entity, EntityId = null, Details = details, IpAddress = "127.0.0.1" });
        Log(T(2026, 11, 1, 10), "bekleyen.danisman@teknoloji.edu.tr", "User.StaffRegistered", nameof(User), "Rol: Danışman Öğretim Elemanı; fakülte onayı bekliyor.");
        Log(T(2026, 11, 2, 11), "bekleyen.danisman2@teknoloji.edu.tr", "User.StaffRegistered", nameof(User), "Rol: Danışman Öğretim Elemanı; fakülte onayı bekliyor.");
        Log(T(2026, 11, 3, 8), elif.User.Email, "WorkPlan.Saved", nameof(MonthlyWorkPlan), "Kasım 2026 planı gönderildi, 6 gün seçildi");
        Log(T(2026, 11, 3, 8, 20), fatma.User.Email, "WorkPlan.Saved", nameof(MonthlyWorkPlan), "Kasım 2026 planı gönderildi, 6 gün seçildi");
        Log(T(2026, 10, 21, 10), admin.Email, "Leave.Approved", nameof(LeaveRequest), "Ahmet Kılıç için 26 Ekim 2026 tarihli 1 günlük izin onaylandı");
    }

    /// <summary>
    /// Her rolün her sayfasında ve butonunda veri görünsün diye eklenen tamamlayıcı kayıtlar:
    /// izinlerin tüm durumları, geri alınmış devamsızlık, okunmuş/okunmamış bildirim, iptal edilmiş devam,
    /// elle eklenmiş devam, tamamlanmamış görev, öğrenciye özel izin hakkı ve denetim izi.
    /// </summary>
    private static void SeedExtras(IskurAutoDbContext db, User admin, User advisor, User advisorMak, User head,
        StudentProfile mehmet, StudentProfile elif, StudentProfile burak, StudentProfile selin, StudentProfile kerem, StudentProfile ahmet,
        MonthlyWorkPlan mehmetOct, MonthlyWorkPlan elifOct, Dictionary<int, AttendanceSession> sessions)
    {
        static DateOnly D(int y, int m, int d) => new(y, m, d);
        static DateTime T(int y, int m, int d, int h, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

        // Elif'e fakülte tarafından özel izin hakkı tanımlanmış.
        elif.LeaveDayAllowance = 12;

        // Burak izin hakkına yaklaşıyor: 3 + 5 gün izin ve 1 gün devamsızlık = 9 / 10.
        db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                StudentProfile = burak, Type = LeaveType.Excuse, Status = LeaveStatus.Approved,
                StartDate = D(2026, 3, 10), EndDate = D(2026, 3, 12), DaysCounted = 3, Reason = "Aile ziyareti",
                AppliedOn = D(2026, 3, 5), ReviewedByUser = admin, ReviewedAt = T(2026, 3, 6, 9)
            },
            new LeaveRequest
            {
                StudentProfile = burak, Type = LeaveType.Health, Status = LeaveStatus.Approved,
                StartDate = D(2026, 6, 1), EndDate = D(2026, 6, 5), DaysCounted = 5, Reason = "Ameliyat sonrası dinlenme",
                AppliedOn = D(2026, 5, 28), ReviewedByUser = admin, ReviewedAt = T(2026, 5, 29, 10),
                ReviewNote = "Sağlık raporu ibraz edildi."
            },
            // Reddedilmiş ve iptal edilmiş talepler
            new LeaveRequest
            {
                StudentProfile = mehmet, Type = LeaveType.Other, Status = LeaveStatus.Rejected,
                StartDate = D(2026, 10, 19), EndDate = D(2026, 10, 20), DaysCounted = 2, Reason = "Kişisel iş",
                AppliedOn = D(2026, 10, 15), ReviewedByUser = admin, ReviewedAt = T(2026, 10, 16, 11),
                ReviewNote = "Plan günlerine denk geldiği için uygun görülmedi."
            },
            new LeaveRequest
            {
                StudentProfile = mehmet, Type = LeaveType.Excuse, Status = LeaveStatus.Cancelled,
                StartDate = D(2026, 10, 28), EndDate = D(2026, 10, 28), DaysCounted = 1, Reason = "Randevu",
                AppliedOn = D(2026, 10, 20)
            },
            // Onay bekleyen yeni bir talep
            new LeaveRequest
            {
                StudentProfile = mehmet, Type = LeaveType.Excuse, Status = LeaveStatus.Pending,
                StartDate = D(2026, 11, 17), EndDate = D(2026, 11, 17), DaysCounted = 1, Reason = "Resmî randevu",
                AppliedOn = D(2026, 10, 22)
            });

        // Geri alınmış bir devamsızlık ve okunmuş bir bildirim
        db.Absences.Add(new Absence
        {
            StudentProfile = mehmet, Date = D(2026, 10, 6), Type = AbsenceType.Unexcused, Note = "Yoklamada görünmedi.",
            RecordedByUser = admin, RevokedAt = T(2026, 10, 7, 9), RevokedByUser = admin,
            RevokeReason = "Öğrenci sağlık raporu getirdi; kayıt hatalıydı."
        });
        db.AbsenceNotices.Add(new AbsenceNotice
        {
            StudentProfile = mehmet, Date = D(2026, 10, 6), Message = "Öğrenci 6 Ekim'de çalışma yerinde görünmedi.",
            AdvisorUser = advisor, CreatedAt = T(2026, 10, 6, 14), ReadAt = T(2026, 10, 7, 8), ReadByUser = admin
        });

        // İptal edilmiş (5 Ekim) ve elle eklenmiş (Elif, 12 Ekim) devam kayıtları
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentProfile = mehmet, AttendanceSession = sessions[5], Date = D(2026, 10, 5), CheckedInAt = T(2026, 10, 5, 6, 20),
            WorkPlanDay = mehmetOct.Days.First(x => x.Date == D(2026, 10, 5)),
            VoidedAt = T(2026, 10, 6, 8), VoidedByUser = admin, VoidReason = "Başka bir öğrenci adına yanlışlıkla okutuldu."
        });
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentProfile = elif, AttendanceSession = sessions[12], Date = D(2026, 10, 12), CheckedInAt = T(2026, 10, 12, 7, 5),
            WorkPlanDay = elifOct.Days.First(x => x.Date == D(2026, 10, 12)),
            IsManual = true, ManualReason = "Telefonu arızalıydı, birimde görüldü.", CreatedByUser = admin
        });

        // Plan örnekleri: raporlar yalnızca onaylı planı saydığı için Kasım 2026 için onaylı planlar da vardır.
        MonthlyWorkPlan AddPlan(StudentProfile s, int year, int month, WorkPlanStatus status, string? note, params int[] days)
        {
            var plan = new MonthlyWorkPlan
            {
                StudentProfile = s, Year = year, Month = month, Status = status, StudentNote = note,
                LastSavedAt = T(year, month - 1, 20, 8)
            };
            if (status == WorkPlanStatus.Approved)
            {
                plan.ReviewedByUser = admin;
                plan.ReviewedAt = T(year, month - 1, 22, 9);
            }
            foreach (var d in days) plan.Days.Add(new WorkPlanDay { Date = D(year, month, d), IsLessonDay = s == mehmet && (D(year, month, d).DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday), Note = null });
            db.WorkPlans.Add(plan);
            return plan;
        }
        AddPlan(selin, 2026, 10, WorkPlanStatus.Pending, "İlk planım.", 6, 8, 13, 15, 20, 22);
        AddPlan(mehmet, 2026, 11, WorkPlanStatus.Approved, null, 3, 5, 17, 19, 24, 26);
        AddPlan(selin, 2026, 11, WorkPlanStatus.Approved, null, 3, 5, 10, 12, 17, 19);
        AddPlan(ahmet, 2026, 11, WorkPlanStatus.Approved, null, 4, 6, 11, 13, 18, 20);
        var burakNov = AddPlan(burak, 2026, 11, WorkPlanStatus.Approved, "Sınav haftalarında da gelebilirim.", 2, 4, 10, 12, 17, 19, 24, 26);

        // 2 Kasım Pazartesi: Burak'ın planlı günü; karekodla devam kaydı yapılmış.
        var novSession = new AttendanceSession
        {
            Date = D(2026, 11, 2), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0),
            Title = "Günlük devam oturumu", SecretKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)),
            CreatedByUser = admin
        };
        db.AttendanceSessions.Add(novSession);
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentProfile = burak, AttendanceSession = novSession, Date = D(2026, 11, 2), CheckedInAt = T(2026, 11, 2, 6, 40),
            WorkPlanDay = burakNov.Days.First(x => x.Date == D(2026, 11, 2))
        });

        // Selin izin ve devamsızlık sınırını aştı: 6 gün onaylı izin + 5 gün devamsızlık = 11 / 10 (ilişik kesme incelemesi listesinde görünür).
        db.LeaveRequests.Add(new LeaveRequest
        {
            StudentProfile = selin, Type = LeaveType.Health, Status = LeaveStatus.Approved,
            StartDate = D(2026, 10, 6), EndDate = D(2026, 10, 13), DaysCounted = 6, Reason = "Grip ve dinlenme",
            AppliedOn = D(2026, 10, 5), ReviewedByUser = admin, ReviewedAt = T(2026, 10, 5, 13), ReviewNote = "Sağlık raporu ibraz edildi."
        });
        foreach (var day in new[] { D(2026, 9, 22), D(2026, 9, 24), D(2026, 9, 29), D(2026, 10, 1), D(2026, 10, 15) })
            db.Absences.Add(new Absence { StudentProfile = selin, Date = day, Type = AbsenceType.Unexcused, Note = "Çalışma yerine gelmedi.", RecordedByUser = admin });

        // Görevler: tamamlanmayan ve açık
        var missed = new StudentTask
        {
            StudentProfile = burak, Title = "Arşiv dosyalarının taranması", Description = "2025 yılı yazışmalarının taranıp klasörlenmesi.",
            AssignedDate = D(2026, 9, 7), DueDate = D(2026, 9, 25), AssignedByUser = advisor,
            Status = StudentTaskStatus.NotCompleted, StatusChangedAt = T(2026, 9, 28, 13)
        };
        missed.Evaluations.Add(new TaskEvaluation
        {
            AuthorUser = advisor, Note = "Süre içinde yarısı tamamlanabildi.", NewStatus = StudentTaskStatus.NotCompleted, CreatedAt = T(2026, 9, 28, 13)
        });
        db.StudentTasks.AddRange(missed, new StudentTask
        {
            StudentProfile = elif, Title = "Yazılım laboratuvarı kurulum listesi", Description = "Laboratuvar bilgisayarlarına kurulacak programların listesi.",
            AssignedDate = D(2026, 10, 12), DueDate = D(2026, 10, 30), AssignedByUser = advisorMak
        });

        db.ExamPeriods.Add(new ExamPeriod { Name = "2025-2026 Bahar dönemi final sınavları", StartDate = D(2026, 6, 8), EndDate = D(2026, 6, 26) });

        // Denetim izi örnekleri
        void Log(DateTime at, User u, string action, string entity, int? id, string? details = null)
            => db.AuditLogs.Add(new AuditLog { OccurredAt = at, UserId = null, UserEmail = u.Email, Action = action, EntityName = entity, EntityId = id, Details = details, IpAddress = "127.0.0.1" });
        Log(T(2026, 10, 16, 11), admin, "Leave.Rejected", nameof(LeaveRequest), null, "19.10.2026 - 20.10.2026 arası, 2 gün");
        Log(T(2026, 10, 7, 9), admin, "Absence.Revoked", nameof(Absence), null, "06.10.2026 tarihli devamsızlık");
        Log(T(2026, 10, 6, 8), admin, "Attendance.Voided", nameof(AttendanceRecord), null, "Başka öğrenci adına okutulan kayıt iptal edildi");
        Log(T(2026, 10, 12, 8), admin, "Attendance.AddedManually", nameof(AttendanceRecord), null, "Telefonu arızalı olan öğrencinin devamı elle eklendi");
        Log(T(2026, 10, 14, 15), advisor, "AbsenceNotice.Sent", nameof(AbsenceNotice), null, "14.10.2026 tarihli devamsızlık bildirimi");
        Log(T(2026, 10, 3, 9), head, "Student.AdvisorAssigned", nameof(StudentProfile), null, "Danışman: Dr. Öğr. Üyesi Zeynep Arslan");
        Log(T(2026, 9, 20, 10), admin, "Student.LeaveAllowanceChanged", nameof(StudentProfile), null, "Öğrenciye özel izin hakkı 12 gün olarak belirlendi.");
        Log(T(2026, 10, 22, 9), admin, "WorkPlan.Approved", nameof(MonthlyWorkPlan), null, "Kasım 2026 planı");
        Log(T(2026, 10, 5, 13), admin, "Leave.Approved", nameof(LeaveRequest), null, "06.10.2026 - 13.10.2026 arası, 6 gün");
        Log(T(2026, 10, 22, 8), mehmet.User, "Leave.Requested", nameof(LeaveRequest), null, "17.11.2026 - 17.11.2026 arası, 1 gün");
        Log(T(2026, 11, 2, 9, 40), burak.User, "Attendance.CheckedIn", nameof(AttendanceRecord), null);
        Log(T(2026, 6, 30, 11), admin, "Termination.Recorded", nameof(TerminationDecision), null);
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
