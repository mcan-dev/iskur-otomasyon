using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Reports;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

/// <summary>
/// Giriş: öğrenci öğrenci e-postası + OBİS şifresiyle, personel sicil numarası + şifresiyle girer.
/// Sistemde kayıt olma yoktur; öğrenci hesabını bilgi işlemden gelen bilgilerle sekreterlik açar.
/// </summary>
public class AuthServiceTests
{
    private static LoginRequest Student(string email, string password) => new() { Username = email, Password = password, Role = UserRole.Student };
    private static LoginRequest Staff(string registry, string password, UserRole role) => new() { Username = registry, Password = password, Role = role };

    private static CreateStudentRequest NewStudent(int departmentId) => new()
    {
        FullName = "  Yeni   Öğrenci ",
        Email = "250000003@Ogr.Selcuk.edu.tr ",
        ObisPassword = "Obis2026",
        PhoneNumber = "0 (532) 111 22 33",
        NationalId = "34567891238",
        StudentNumber = "250000003",
        DepartmentId = departmentId,
        RegistrationYear = 2025
    };

    // ─── Öğrenci hesabını sekreterlik açar ────────────────────────────────────

    [Fact]
    public async Task CreateStudent_ByFaculty_CreatesUserProfileAndOpenParticipation_AndStudentCanLogInWithObisPassword()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            var created = await scope.Students.CreateAsync(NewStudent(seed.BmId));
            Assert.Equal("250000003@ogr.selcuk.edu.tr", created.Email);
        }

        using (var verify = db.NewContext())
        {
            var profile = await verify.StudentProfiles
                .Include(s => s.User).Include(s => s.Participations)
                .SingleAsync(s => s.StudentNumber == "250000003");

            Assert.Equal(UserRole.Student, profile.User.Role);
            Assert.Equal("Yeni Öğrenci", profile.User.FullName);
            Assert.Equal("05321112233", profile.User.PhoneNumber);
            Assert.NotEqual("Obis2026", profile.User.PasswordHash);    // şifre açık saklanmaz
            Assert.Null(Assert.Single(profile.Participations).EndDate);
        }

        db.Current.Anonymous();
        using var login = db.NewScope();
        var response = await login.Auth.LoginAsync(Student("250000003@ogr.selcuk.edu.tr", "Obis2026"));
        Assert.Equal("Student", response.User.Role);
    }

    [Fact]
    public async Task CreateStudent_NewStudent_IsVisibleToDepartmentAndInReports()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            await scope.Students.CreateAsync(NewStudent(seed.BmId));
            var report = await scope.Reports.GetAsync(ReportKind.StudentList, new ReportQuery());
            Assert.Contains(report.Rows, r => r.Contains("250000003"));
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Equal(1, (await scope.Students.SearchAsync(new StudentSearchQuery { Text = "250000003" })).TotalCount);
    }

    [Fact]
    public async Task CreateStudent_ByNonFaculty_IsForbidden()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Students.CreateAsync(NewStudent(seed.BmId)));
    }

    [Fact]
    public async Task CreateStudent_WithManyInvalidFields_ReportsAllErrorsAtOnce()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        var request = NewStudent(seed.BmId);
        request.NationalId = "12345678901";
        request.ObisPassword = "x";
        request.PhoneNumber = "123";
        request.DepartmentId = 9999;
        request.RegistrationYear = 2099;

        using var scope = db.NewScope();
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.CreateAsync(request));

        foreach (var field in new[] { "NationalId", "ObisPassword", "PhoneNumber", "DepartmentId", "RegistrationYear" })
            Assert.True(ex.Errors.ContainsKey(field), $"'{field}' hatası bekleniyordu.");
    }

    [Theory]
    [InlineData("email")]
    [InlineData("nationalId")]
    [InlineData("studentNumber")]
    public async Task CreateStudent_WithDuplicateUniqueValue_ThrowsConflict_AndLeavesNoPartialRecords(string duplicatedField)
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        var request = NewStudent(seed.BmId);
        switch (duplicatedField)
        {
            case "email": request.Email = "mehmet@test.local"; break;
            case "nationalId": request.NationalId = "12345678950"; break;
            case "studentNumber": request.StudentNumber = "230000001"; break;
        }

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Students.CreateAsync(request));

        using var verify = db.NewContext();
        Assert.Equal(2, await verify.StudentProfiles.CountAsync());
    }

    // ─── Öğrenci girişi: e-posta + OBİS şifresi ───────────────────────────────

    [Fact]
    public async Task StudentLogin_WithEmailAndObisPassword_ReturnsTokenAndSetsLastLogin()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        using (var scope = db.NewScope())
        {
            var response = await scope.Auth.LoginAsync(Student(" MEHMET@test.local ", TestData.Password));
            Assert.False(string.IsNullOrEmpty(response.AccessToken));
            Assert.Equal("Student", response.User.Role);
        }

        using var verify = db.NewContext();
        Assert.Equal(db.Clock.UtcNow, (await verify.Users.SingleAsync(u => u.Email == "mehmet@test.local")).LastLoginAt);
    }

    [Fact]
    public async Task StudentLogin_WithWrongPassword_ThrowsAndWritesAuditRecord()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        using (var scope = db.NewScope())
        {
            var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() => scope.Auth.LoginAsync(Student("mehmet@test.local", "yanlis-sifre1")));
            Assert.Contains("OBİS", ex.Message);
        }

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LoginFailed));
    }

    [Fact]
    public async Task StudentLogin_UnknownEmailAndWrongPassword_GiveIdenticalMessage()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        using var scope = db.NewScope();

        var unknown = await Assert.ThrowsAsync<AuthenticationFailedException>(() => scope.Auth.LoginAsync(Student("yok@test.local", "Parola123")));
        var wrong = await Assert.ThrowsAsync<AuthenticationFailedException>(() => scope.Auth.LoginAsync(Student("mehmet@test.local", "Parola123")));

        Assert.Equal(unknown.Message, wrong.Message);
    }

    // ─── Personel girişi: sicil numarası ──────────────────────────────────────

    [Fact]
    public async Task StaffLogin_WithRegistryNumber_Succeeds_ButEmailIsNotAccepted()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        using var scope = db.NewScope();

        var response = await scope.Auth.LoginAsync(Staff(" 300001 ", TestData.Password, UserRole.Advisor));
        Assert.Equal("Advisor", response.User.Role);
        Assert.Equal("300001", response.User.RegistryNumber);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            scope.Auth.LoginAsync(Staff("advisor@test.local", TestData.Password, UserRole.Advisor)));
    }

    [Fact]
    public async Task StaffLogin_WithWrongPassword_SaysSicilOrPasswordIsWrong()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        using var scope = db.NewScope();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() => scope.Auth.LoginAsync(Staff("100001", "Yanlis123", UserRole.FacultyAdmin)));
        Assert.Contains("Sicil", ex.Message);
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
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => scope.Auth.LoginAsync(Staff("300001", TestData.Password, UserRole.Advisor)));
        Assert.Contains("Hesabınız aktif değil", ex.Message);

        // Yanlış parolada hesabın durumu açıklanmaz.
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => scope.Auth.LoginAsync(Staff("300001", "Yanlis123", UserRole.Advisor)));
    }

    [Fact]
    public async Task Login_ThroughAnotherRolesPage_IsRejectedAndNoTokenIsIssued()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Auth.LoginAsync(Staff("300001", TestData.Password, UserRole.FacultyAdmin)));

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.LoginFailed));
        Assert.Null((await verify.Users.SingleAsync(u => u.RegistryNumber == "300001")).LastLoginAt);
    }

    // ─── Şifre değiştirme ─────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ForStaff_WithCorrectCurrent_ChangesPassword()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);

        using (var scope = db.NewScope())
            await scope.Auth.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = TestData.Password, NewPassword = "YeniParola9" });

        db.Current.Anonymous();
        using var login = db.NewScope();
        await login.Auth.LoginAsync(Staff("300001", "YeniParola9", UserRole.Advisor));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => login.Auth.LoginAsync(Staff("300001", TestData.Password, UserRole.Advisor)));
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

    [Fact]
    public async Task ChangePassword_ForStudent_IsForbidden_BecauseItIsTheObisPassword()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using var scope = db.NewScope();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            scope.Auth.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = TestData.Password, NewPassword = "YeniParola9" }));
        Assert.Contains("OBİS", ex.Message);
    }
}
