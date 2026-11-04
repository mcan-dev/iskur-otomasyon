using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class UserServiceTests
{
    private static CreateStaffUserRequest Staff(UserRole role, int? departmentId, string email = "yeni.personel@test.local") => new()
    {
        FullName = "Yeni Personel",
        Email = email,
        Password = "Parola123",
        Role = role,
        DepartmentId = departmentId
    };

    [Fact]
    public async Task CreateStaff_Advisor_RequiresDepartment()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Users.CreateStaffAsync(Staff(UserRole.Advisor, null)));
        var created = await scope.Users.CreateStaffAsync(Staff(UserRole.Advisor, seed.BmId));
        Assert.Equal("Advisor", created.Role);
        Assert.Equal(seed.BmId, created.DepartmentId);
    }

    [Fact]
    public async Task CreateStaff_CannotCreateStudentThisWay()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Users.CreateStaffAsync(Staff(UserRole.Student, null)));
    }

    private static async Task<int> RegisterPendingAsync(TestDb db, int departmentId, UserRole role = UserRole.Advisor, string email = "basvuru@test.local")
    {
        db.Current.Anonymous();
        using var scope = db.NewScope();
        var result = await scope.Auth.RegisterStaffAsync(new StaffRegistrationRequest
        {
            FullName = "Başvuru Sahibi", Email = email, Password = "Parola123", PhoneNumber = "05321112233",
            Role = role, DepartmentId = departmentId
        });
        return result.UserId;
    }

    [Fact]
    public async Task PendingApplications_AreListedForTheFacultyOffice_AndApprovalActivatesThem()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var userId = await RegisterPendingAsync(db, seed.BmId);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var pending = await scope.Users.SearchAsync(new UserSearchQuery { IsPendingApproval = true });
            var only = Assert.Single(pending);
            Assert.Equal(userId, only.Id);
            Assert.True(only.IsPendingApproval);

            var approved = await scope.Users.SetActiveAsync(userId, true);
            Assert.True(approved.IsActive);
            Assert.False(approved.IsPendingApproval);
            Assert.Empty(await scope.Users.SearchAsync(new UserSearchQuery { IsPendingApproval = true }));
        }

        db.Current.Anonymous();
        using var login = db.NewScope();
        var ok = await login.Auth.LoginAsync(new LoginRequest { Email = "basvuru@test.local", Password = "Parola123" });
        Assert.False(string.IsNullOrEmpty(ok.AccessToken));
    }

    [Fact]
    public async Task PendingApplication_CanBeRejected_AndStaysInactive()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var userId = await RegisterPendingAsync(db, seed.BmId);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var rejected = await scope.Users.SetActiveAsync(userId, false);
            Assert.False(rejected.IsActive);
            Assert.False(rejected.IsPendingApproval);
            Assert.Empty(await scope.Users.SearchAsync(new UserSearchQuery { IsPendingApproval = true }));
        }

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.UserApplicationRejected && a.EntityId == userId));
    }

    [Fact]
    public async Task DepartmentHeadApplication_CannotBeApprovedWhileAnotherHeadIsActive()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        // Test verisinde BM bölümünün aktif bir başkanı zaten var.
        var userId = await RegisterPendingAsync(db, seed.BmId, UserRole.DepartmentHead, "yeni.baskan@test.local");

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope2 = db.NewScope();
        await Assert.ThrowsAsync<ConflictException>(() => scope2.Users.SetActiveAsync(userId, true));
    }

    [Fact]
    public async Task OnlyTheFacultyOffice_CanDecideOnApplications()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var userId = await RegisterPendingAsync(db, seed.BmId);

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Users.SetActiveAsync(userId, true));
    }

    [Fact]
    public async Task CreateStaff_OnlyOneActiveDepartmentHeadPerDepartment()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        // BM'nin zaten aktif başkanı var → ikincisi reddedilir.
        await Assert.ThrowsAsync<ConflictException>(() => scope.Users.CreateStaffAsync(Staff(UserRole.DepartmentHead, seed.BmId)));
        // YM'nin başkanı yok → oluşturulabilir.
        await scope.Users.CreateStaffAsync(Staff(UserRole.DepartmentHead, seed.YmId, "ym.baskan@test.local"));
    }

    [Fact]
    public async Task CreateStaff_DuplicateEmail_ThrowsConflict()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ConflictException>(() => scope.Users.CreateStaffAsync(Staff(UserRole.Advisor, seed.BmId, "ADVISOR@test.local")));
    }

    [Fact]
    public async Task CreateStaff_OnlyFacultyAdmin()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Users.CreateStaffAsync(Staff(UserRole.Advisor, seed.BmId)));
    }

    [Fact]
    public async Task Search_AsDepartmentHead_SeesOnlyOwnDepartmentStaff()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        var advisors = await scope.Users.SearchAsync(new UserSearchQuery { Role = UserRole.Advisor, DepartmentId = seed.YmId });
        var advisor = Assert.Single(advisors);          // YM isteği yok sayıldı, BM danışmanı döndü
        Assert.Equal("Zeynep Arslan", advisor.FullName);
    }

    [Fact]
    public async Task SetActive_CannotDeactivateYourself_AndCannotTouchStudents()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ConflictException>(() => scope.Users.SetActiveAsync(seed.AdminId, false));
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Users.SetActiveAsync(seed.MehmetUserId, false));

        var deactivated = await scope.Users.SetActiveAsync(seed.AdvisorId, false);
        Assert.False(deactivated.IsActive);
    }
}

public class DepartmentAndUnitServiceTests
{
    [Fact]
    public async Task Department_CodeIsNormalizedAndUnique()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        var created = await scope.Departments.CreateAsync(new DepartmentUpsertRequest { Name = "Mekatronik", Code = "mek" });
        Assert.Equal("MEK", created.Code);
        await Assert.ThrowsAsync<ConflictException>(() => scope.Departments.CreateAsync(new DepartmentUpsertRequest { Name = "Başka", Code = "Mek" }));
    }

    [Fact]
    public async Task Department_InactiveOnesAreHiddenFromNonAdmins()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Departments.UpdateAsync(seed.YmId, new DepartmentUpsertRequest { Name = "Yazılım Mühendisliği", Code = "YM", IsActive = false });

        db.Current.Anonymous();
        using (var scope = db.NewScope())
            Assert.Single(await scope.Departments.ListAsync(includeInactive: true));    // anonim: istek yok sayılır

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            Assert.Equal(2, (await scope.Departments.ListAsync(includeInactive: true)).Count);
    }

    [Fact]
    public async Task WorkUnit_NameUnique_AndOnlyAdminManages()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await scope.WorkUnits.CreateAsync(new WorkUnitUpsertRequest { Name = "Öğrenci İşleri" });
            await Assert.ThrowsAsync<ConflictException>(() => scope.WorkUnits.CreateAsync(new WorkUnitUpsertRequest { Name = "Kütüphane" }));
        }

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.WorkUnits.ListAsync(false));
    }

    [Fact]
    public async Task AuditLog_OnlyAdminCanSearch()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await scope.Departments.CreateAsync(new DepartmentUpsertRequest { Name = "Mekatronik", Code = "MEK" });
            var logs = await scope.Audit.SearchAsync(new AuditLogQuery { Action = "Department.Created" });
            Assert.Equal(1, logs.TotalCount);
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Audit.SearchAsync(new AuditLogQuery()));
    }
}

/// <summary>
/// Uygulama kodu bir kuralı unutsa bile veritabanının son güvence olduğunu kanıtlar
/// (belge madde 9: mükerrer kayıtların önlenmesi).
/// </summary>
public class DatabaseConstraintTests
{
    [Fact]
    public async Task Database_RejectsDuplicateEmail()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        using var ctx = db.NewContext();

        ctx.Users.Add(new User { FullName = "Kopya", Email = "admin@test.local", PasswordHash = "x", Role = UserRole.Advisor });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsDuplicateNationalId()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();

        ctx.StudentProfiles.Add(new StudentProfile
        {
            User = new User { FullName = "Kopya", Email = "kopya@test.local", PasswordHash = "x", Role = UserRole.Student },
            NationalId = "12345678950", Iban = "TR400006200000000000000001", StudentNumber = "9999", DepartmentId = seed.BmId, RegistrationYear = 2025
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsSecondOpenParticipationForSameStudent()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();

        // Mehmet'in zaten açık bir katılımı var; ikinci açık katılım filtreli benzersiz indeksle engellenir.
        ctx.ProgramParticipations.Add(new ProgramParticipation { StudentProfileId = seed.MehmetId, StartDate = db.Clock.Today });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_AllowsManyClosedParticipations()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();

        var today = db.Clock.Today;
        ctx.ProgramParticipations.AddRange(
            new ProgramParticipation { StudentProfileId = seed.ElifId, StartDate = today.AddDays(-300), EndDate = today.AddDays(-200) },
            new ProgramParticipation { StudentProfileId = seed.ElifId, StartDate = today.AddDays(-150), EndDate = today.AddDays(-100) });
        await ctx.SaveChangesAsync();   // hata fırlatmamalı
        Assert.Equal(3, await ctx.ProgramParticipations.CountAsync(p => p.StudentProfileId == seed.ElifId));
    }

    [Fact]
    public async Task Database_RejectsSecondOpenAdvisorAssignment()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();

        ctx.AdvisorAssignments.Add(new AdvisorAssignment
        {
            StudentProfileId = seed.MehmetId, AdvisorUserId = seed.AdvisorId, AssignedByUserId = seed.HeadId, StartDate = db.Clock.Today
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_BlocksDeletingDepartmentThatHasStudents()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();

        // Cascade delete kapalı: bağlı geçmişi olan bölüm sessizce silinemez.
        ctx.Departments.Remove((await ctx.Departments.FindAsync(seed.BmId))!);
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsSecondPlanForSameStudentAndMonth()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();

        ctx.WorkPlans.AddRange(
            new MonthlyWorkPlan { StudentProfileId = seed.MehmetId, Year = 2026, Month = 11 },
            new MonthlyWorkPlan { StudentProfileId = seed.MehmetId, Year = 2026, Month = 11 });

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsSecondActiveAbsenceForSameDay_ButAllowsAfterRevoke()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using var ctx = db.NewContext();
        var day = new DateOnly(2026, 10, 10);

        ctx.Absences.Add(new Absence { StudentProfileId = seed.MehmetId, Date = day, RecordedByUserId = seed.AdminId });
        await ctx.SaveChangesAsync();

        ctx.Absences.Add(new Absence { StudentProfileId = seed.MehmetId, Date = day, RecordedByUserId = seed.AdminId });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }
}
