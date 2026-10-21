using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using IskurAuto.Data.Repositories;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Implementations;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Tests.Support;

/// <summary>Sabit zaman: testler "bugün" tarihine bağlı kalmaz, sonuçlar her gün aynıdır.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.AddHours(3));
    public void AdvanceDays(int days) => UtcNow = UtcNow.AddDays(days);
}

/// <summary>"İsteği yapan kullanıcı" taklidi. Testte rol değiştirmek tek satırdır: <c>Current.As(...)</c>.</summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; private set; }
    public int UserId { get; private set; }
    public UserRole? Role { get; private set; }
    public int? DepartmentId { get; private set; }
    public string? Email { get; private set; }
    public string? IpAddress => "127.0.0.1";

    public void As(UserRole role, int userId, int? departmentId = null, string? email = null)
    {
        IsAuthenticated = true;
        Role = role;
        UserId = userId;
        DepartmentId = departmentId;
        Email = email ?? $"user{userId}@test.local";
    }

    public void Anonymous()
    {
        IsAuthenticated = false;
        Role = null;
        UserId = 0;
        DepartmentId = null;
        Email = null;
    }
}

public sealed class FakeTokenService : ITokenService
{
    public TokenResult CreateToken(User user) => new($"token-for-{user.Id}", DateTime.UtcNow.AddHours(1));
}

/// <summary>
/// Her test kendi izole, bellek içi SQLite veritabanını kurar.
/// Bağlantı açık kaldığı sürece veritabanı yaşar; test bitince Dispose ile yok olur.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IskurAutoDbContext> _options;

    public FakeClock Clock { get; } = new(new DateTime(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc));
    public FakeCurrentUser Current { get; } = new();

    /// <summary>Testlerin hızlı çalışması için düşük yinelemeli hasher (üretimde 600.000).</summary>
    public IPasswordHasher Hasher { get; } = new Pbkdf2PasswordHasher(iterations: 1_000);

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<IskurAutoDbContext>().UseSqlite(_connection).Options;

        using var context = new IskurAutoDbContext(_options);
        context.Database.EnsureCreated();
    }

    public IskurAutoDbContext NewContext() => new(_options);

    /// <summary>
    /// Yeni bir "HTTP isteği" gibi davranır: taze DbContext + taze servisler.
    /// Önceki isteğin izlenen nesneleri taşınmadığı için eksik Include gibi hatalar gizlenemez.
    /// </summary>
    public TestScope NewScope() => new(this, NewContext());

    public void Dispose() => _connection.Dispose();
}

/// <summary>Bir "istek" boyunca birlikte yaşayan context, repository'ler ve servisler.</summary>
public sealed class TestScope : IDisposable
{
    public IskurAutoDbContext Context { get; }
    public IAuthService Auth { get; }
    public IStudentService Students { get; }
    public IUserService Users { get; }
    public IDepartmentService Departments { get; }
    public IWorkUnitService WorkUnits { get; }
    public IAuditService Audit { get; }

    public TestScope(TestDb db, IskurAutoDbContext context)
    {
        Context = context;

        var users = new UserRepository(context);
        var students = new StudentRepository(context);
        var departments = new DepartmentRepository(context);
        var units = new WorkUnitRepository(context);
        var audit = new AuditService(new AuditLogRepository(context), db.Clock, db.Current);

        Audit = audit;
        Auth = new AuthService(users, students, departments, context, db.Hasher, new FakeTokenService(), db.Clock, audit, db.Current);
        Students = new StudentService(students, users, units, context, db.Clock, audit, db.Current);
        Users = new UserService(users, departments, context, db.Hasher, audit, db.Current);
        Departments = new DepartmentService(departments, context, audit, db.Current);
        WorkUnits = new WorkUnitService(units, context, audit, db.Current);
    }

    public void Dispose() => Context.Dispose();
}

/// <summary>Testlerin ortak başlangıç verisi (Id'ler kaydedildikten sonra okunabilir).</summary>
public sealed class SeedResult
{
    public int BmId { get; init; }
    public int YmId { get; init; }
    public int AdminId { get; init; }
    public int HeadId { get; init; }          // BM bölüm başkanı
    public int AdvisorId { get; init; }       // BM danışmanı
    public int AdvisorYmId { get; init; }     // YM danışmanı
    public int LabId { get; init; }
    public int LibraryId { get; init; }
    public int MehmetId { get; init; }        // BM öğrencisi, laboratuvar + danışman atanmış
    public int MehmetUserId { get; init; }
    public int ElifId { get; init; }          // YM öğrencisi, atama yok
    public int ElifUserId { get; init; }
}

public static class TestData
{
    public const string Password = "Demo1234!";

    public static SeedResult Seed(TestDb db)
    {
        using var ctx = db.NewContext();
        var hash = db.Hasher.Hash(Password);
        var start = db.Clock.Today.AddDays(-30);

        var bm = new Department { Name = "Bilgisayar Mühendisliği", Code = "BM" };
        var ym = new Department { Name = "Yazılım Mühendisliği", Code = "YM" };
        var lab = new WorkUnit { Name = "Bilgisayar Laboratuvarı" };
        var lib = new WorkUnit { Name = "Kütüphane" };

        var admin = new User { FullName = "Ayşe Kaya", Email = "admin@test.local", PasswordHash = hash, Role = UserRole.FacultyAdmin };
        var head = new User { FullName = "Mehmet Demir", Email = "head@test.local", PasswordHash = hash, Role = UserRole.DepartmentHead, Department = bm };
        var advisor = new User { FullName = "Zeynep Arslan", Email = "advisor@test.local", PasswordHash = hash, Role = UserRole.Advisor, Department = bm };
        var advisorYm = new User { FullName = "Can Er", Email = "advisor.ym@test.local", PasswordHash = hash, Role = UserRole.Advisor, Department = ym };

        var mehmet = NewStudent("Mehmet Öztürk", "mehmet@test.local", "12345678950", "TR400006200000000000000001", "2023000001", bm, hash, start);
        var elif = NewStudent("Elif Yıldız", "elif@test.local", "23456789138", "TR130006200000000000000002", "2024000002", ym, hash, start);

        mehmet.UnitAssignments.Add(new StudentUnitAssignment { WorkUnit = lab, StartDate = start, AssignedByUser = admin });
        mehmet.AdvisorAssignments.Add(new AdvisorAssignment { AdvisorUser = advisor, StartDate = start, AssignedByUser = head });

        ctx.AddRange(bm, ym, lab, lib, admin, head, advisor, advisorYm, mehmet, elif);
        ctx.SaveChanges();

        return new SeedResult
        {
            BmId = bm.Id, YmId = ym.Id, AdminId = admin.Id, HeadId = head.Id,
            AdvisorId = advisor.Id, AdvisorYmId = advisorYm.Id, LabId = lab.Id, LibraryId = lib.Id,
            MehmetId = mehmet.Id, MehmetUserId = mehmet.UserId, ElifId = elif.Id, ElifUserId = elif.UserId
        };
    }

    private static StudentProfile NewStudent(string name, string email, string nationalId, string iban,
        string number, Department department, string hash, DateOnly start)
    {
        var profile = new StudentProfile
        {
            User = new User { FullName = name, Email = email, PhoneNumber = "05321234567", PasswordHash = hash, Role = UserRole.Student },
            NationalId = nationalId,
            Iban = iban,
            StudentNumber = number,
            Department = department,
            RegistrationYear = 2023
        };
        profile.Participations.Add(new ProgramParticipation { StartDate = start });
        return profile;
    }
}
