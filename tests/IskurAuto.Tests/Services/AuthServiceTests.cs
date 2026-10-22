using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class AuthServiceTests
{
    private static StudentRegistrationRequest ValidRegistration(int departmentId) => new()
    {
        FullName = "  Yeni   Öğrenci ",
        Email = "Yeni@Ogr.edu.tr ",
        Password = "Parola123",
        PhoneNumber = "0 (532) 111 22 33",
        NationalId = "34567891238",
        Iban = "tr83 0006 2000 0000 0000 0000 03",
        StudentNumber = "2025000003",
        DepartmentId = departmentId,
        RegistrationYear = 2025
    };

    // ─── Öğrenci kaydı ────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_CreatesUserProfileAndOpenParticipation()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        using (var scope = db.NewScope())
        {
            var result = await scope.Auth.RegisterStudentAsync(ValidRegistration(seed.BmId));
            Assert.Equal("yeni@ogr.edu.tr", result.Email);
        }

        using var verify = db.NewContext();
        var profile = await verify.StudentProfiles
            .Include(s => s.User).Include(s => s.Participations)
            .SingleAsync(s => s.StudentNumber == "2025000003");

        Assert.Equal(UserRole.Student, profile.User.Role);
        Assert.Equal("Yeni Öğrenci", profile.User.FullName);          // boşluklar düzeltildi
        Assert.Equal("05321112233", profile.User.PhoneNumber);        // telefon normalize
        Assert.Equal("TR830006200000000000000003", profile.Iban);     // IBAN normalize
        Assert.NotEqual("Parola123", profile.User.PasswordHash);      // parola açık saklanmaz
        var participation = Assert.Single(profile.Participations);
        Assert.Null(participation.EndDate);                           // katılım açık = aktif
        Assert.Equal(db.Clock.Today, participation.StartDate);
    }

    [Fact]
    public async Task Register_WithManyInvalidFields_ReportsAllErrorsAtOnce()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var request = ValidRegistration(seed.BmId);
        request.NationalId = "12345678901";
        request.Iban = "TR000000000000000000000000";
        request.Password = "kisa";
        request.PhoneNumber = "123";
        request.DepartmentId = 9999;

        using var scope = db.NewScope();
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Auth.RegisterStudentAsync(request));

        foreach (var field in new[] { "NationalId", "Iban", "Password", "PhoneNumber", "DepartmentId" })
            Assert.True(ex.Errors.ContainsKey(field), $"'{field}' hatası bekleniyordu.");
    }

    [Fact]
    public async Task Register_WithFutureRegistrationYear_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var request = ValidRegistration(seed.BmId);
        request.RegistrationYear = 2099;

        using var scope = db.NewScope();
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Auth.RegisterStudentAsync(request));
        Assert.True(ex.Errors.ContainsKey("RegistrationYear"));
    }

    [Theory]
    [InlineData("email")]
    [InlineData("nationalId")]
    [InlineData("studentNumber")]
    public async Task Register_WithDuplicateUniqueValue_ThrowsConflict(string duplicatedField)
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        // Var olan öğrenci Mehmet'in değerlerinden birini kullan.
        var request = ValidRegistration(seed.BmId);
        switch (duplicatedField)
        {
            case "email": request.Email = "mehmet@test.local"; break;
            case "nationalId": request.NationalId = "12345678950"; break;
            case "studentNumber": request.StudentNumber = "2023000001"; break;
        }

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ConflictException>(() => scope.Auth.RegisterStudentAsync(request));
    }

    [Fact]
    public async Task Register_WhenFails_LeavesNoPartialRecords()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var request = ValidRegistration(seed.BmId);
        request.NationalId = "12345678950";   // mükerrer → hata

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Auth.RegisterStudentAsync(request));

        using var verify = db.NewContext();
        Assert.False(await verify.Users.AnyAsync(u => u.Email == "yeni@ogr.edu.tr"));
    }

    // ─── Giriş ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndSetsLastLogin()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        using (var scope = db.NewScope())
        {
            var response = await scope.Auth.LoginAsync(new LoginRequest { Email = " ADMIN@test.local ", Password = TestData.Password });
            Assert.False(string.IsNullOrEmpty(response.AccessToken));
            Assert.Equal("FacultyAdmin", response.User.Role);
        }

        using var verify = db.NewContext();
        var user = await verify.Users.SingleAsync(u => u.Email == "admin@test.local");
        Assert.Equal(db.Clock.UtcNow, user.LastLoginAt);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsAndWritesAuditRecord()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
                scope.Auth.LoginAsync(new LoginRequest { Email = "admin@test.local", Password = "yanlis-parola1" }));

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LoginFailed));
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_GiveIdenticalMessage()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        using var scope = db.NewScope();

        var unknown = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            scope.Auth.LoginAsync(new LoginRequest { Email = "yok@test.local", Password = "Parola123" }));
        var wrong = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            scope.Auth.LoginAsync(new LoginRequest { Email = "admin@test.local", Password = "Parola123" }));

        Assert.Equal(unknown.Message, wrong.Message);   // hangisinin yanlış olduğu sızdırılmaz
    }

    [Fact]
    public async Task Login_WithInactiveUser_IsRejectedEvenWithCorrectPassword()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using (var ctx = db.NewContext())
        {
            (await ctx.Users.FindAsync(seed.AdvisorId))!.IsActive = false;
            await ctx.SaveChangesAsync();
        }

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            scope.Auth.LoginAsync(new LoginRequest { Email = "advisor@test.local", Password = TestData.Password }));
    }

    [Fact]
    public async Task Login_WithMatchingRole_Succeeds()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        using var scope = db.NewScope();

        var response = await scope.Auth.LoginAsync(new LoginRequest
        {
            Email = "advisor@test.local", Password = TestData.Password, Role = UserRole.Advisor
        });

        Assert.Equal("Advisor", response.User.Role);
    }

    [Fact]
    public async Task Login_ThroughAnotherRolesPage_IsRejectedAndNoTokenIsIssued()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        using (var scope = db.NewScope())
        {
            // Danışman hesabı, fakülte giriş sayfasından girmeye çalışıyor.
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Auth.LoginAsync(new LoginRequest
            {
                Email = "advisor@test.local", Password = TestData.Password, Role = UserRole.FacultyAdmin
            }));
        }

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LoginFailed));
        Assert.Null((await verify.Users.SingleAsync(u => u.Email == "advisor@test.local")).LastLoginAt);
    }

    // ─── Parola değiştirme ────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WithCorrectCurrent_ChangesPassword()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);

        using (var scope = db.NewScope())
            await scope.Auth.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = TestData.Password, NewPassword = "YeniParola9" });

        db.Current.Anonymous();
        using var login = db.NewScope();
        await login.Auth.LoginAsync(new LoginRequest { Email = "advisor@test.local", Password = "YeniParola9" });
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            login.Auth.LoginAsync(new LoginRequest { Email = "advisor@test.local", Password = TestData.Password }));
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentOrWeakNew_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Auth.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = "yanlis", NewPassword = "YeniParola9" }));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Auth.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = TestData.Password, NewPassword = "12345678" }));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Auth.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = TestData.Password, NewPassword = TestData.Password }));
    }
}
