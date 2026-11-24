using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

/// <summary>
/// Belge madde 2 ve 11: "Her kullanıcı yalnızca kendi görev ve yetkileri kapsamında işlem yapabilir",
/// "Öğrenciler başka öğrencilerin kişisel bilgilerini görüntüleyememelidir".
/// </summary>
public class StudentVisibilityTests
{
    [Fact]
    public async Task Search_AsFacultyAdmin_SeesAllStudents()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        var page = await scope.Students.SearchAsync(new StudentSearchQuery());
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Search_AsDepartmentHead_IsLimitedToOwnDepartment_EvenWhenFilterIsSpoofed()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);

        using var scope = db.NewScope();
        // İstemci başka bölümün Id'sini yollayarak sınırı aşmaya çalışıyor:
        var page = await scope.Students.SearchAsync(new StudentSearchQuery { DepartmentId = seed.YmId });

        var item = Assert.Single(page.Items);
        Assert.Equal("Mehmet Öztürk", item.FullName);
    }

    [Fact]
    public async Task Search_AsAdvisor_SeesOnlyAssignedStudents()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Single((await scope.Students.SearchAsync(new StudentSearchQuery())).Items);

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);   // öğrencisi olmayan danışman
        using (var scope = db.NewScope())
            Assert.Empty((await scope.Students.SearchAsync(new StudentSearchQuery())).Items);
    }

    [Fact]
    public async Task Search_AsStudent_IsForbidden()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Students.SearchAsync(new StudentSearchQuery()));
    }

    [Fact]
    public async Task Search_ListMasksNationalId()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        var item = (await scope.Students.SearchAsync(new StudentSearchQuery { Text = "Mehmet" })).Items.Single();
        Assert.Equal("123******50", item.MaskedNationalId);
        Assert.Equal("Bilgisayar Laboratuvarı", item.CurrentUnitName);
        Assert.Equal("Zeynep Arslan", item.CurrentAdvisorName);
    }

    [Fact]
    public async Task Search_FiltersByActiveStatusAndText()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        Assert.Equal(2, (await scope.Students.SearchAsync(new StudentSearchQuery { IsActive = true })).TotalCount);
        Assert.Equal(0, (await scope.Students.SearchAsync(new StudentSearchQuery { IsActive = false })).TotalCount);
        Assert.Equal(1, (await scope.Students.SearchAsync(new StudentSearchQuery { Text = "240000002" })).TotalCount);
        Assert.Equal(0, (await scope.Students.SearchAsync(new StudentSearchQuery { Text = "%" })).TotalCount);   // joker değil, düz karakter
    }

    [Fact]
    public async Task Search_PagingReturnsRequestedPage()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        var page = await scope.Students.SearchAsync(new StudentSearchQuery { Page = 2, PageSize = 1 });
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task GetById_StudentCannotSeeAnotherStudent_NotFoundHidesExistence()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<NotFoundException>(() => scope.Students.GetByIdAsync(seed.ElifId));
        var own = await scope.Students.GetByIdAsync(seed.MehmetId);
        Assert.False(own.IsPersonalDataMasked);
    }

    [Fact]
    public async Task GetById_AsDepartmentHead_OtherDepartmentIsNotFound_OwnIsMasked()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<NotFoundException>(() => scope.Students.GetByIdAsync(seed.ElifId));

        var detail = await scope.Students.GetByIdAsync(seed.MehmetId);
        Assert.True(detail.IsPersonalDataMasked);
        Assert.Contains("*", detail.NationalId);
        Assert.Contains("*", detail.Iban);
    }

    [Fact]
    public async Task GetById_AsAdvisor_OnlyAssignedStudent()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);

        using var scope = db.NewScope();
        await scope.Students.GetByIdAsync(seed.MehmetId);
        await Assert.ThrowsAsync<NotFoundException>(() => scope.Students.GetByIdAsync(seed.ElifId));
    }

    [Fact]
    public async Task GetById_AsFacultyAdmin_ShowsRawDataAndWritesAuditRecord()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            var detail = await scope.Students.GetByIdAsync(seed.MehmetId);
            Assert.Equal("12345678950", detail.NationalId);
            Assert.False(detail.IsPersonalDataMasked);
        }

        using var verify = db.NewContext();
        var log = await verify.AuditLogs.SingleAsync(a => a.Action == AuditActions.PersonalDataViewed);
        Assert.Equal(seed.AdminId, log.UserId);
        Assert.Equal(seed.MehmetId, log.EntityId);
        Assert.DoesNotContain("12345678950", log.Details ?? string.Empty);   // denetim kaydında kişisel veri yok
    }

    [Fact]
    public async Task UpdateMyProfile_NormalizesAndValidates()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);

        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.Students.UpdateMyProfileAsync(new UpdateMyProfileRequest { PhoneNumber = "123", Iban = "TR00", IbanHolderName = "Mehmet Öztürk", ConfirmOwnIban = true }));

            var updated = await scope.Students.UpdateMyProfileAsync(new UpdateMyProfileRequest
            {
                PhoneNumber = "+90 533 000 11 22",
                Iban = "tr13 0006 2000 0000 0000 0000 02",
                IbanHolderName = " mehmet  ÖZTÜRK ",
                ConfirmOwnIban = true
            });
            Assert.Equal("05330001122", updated.PhoneNumber);
            Assert.Equal("TR130006200000000000000002", updated.Iban);
        }
    }

    [Fact]
    public async Task UpdateMyProfile_IbanMustBelongToTheStudent()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using var scope = db.NewScope();

        var otherName = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.UpdateMyProfileAsync(new UpdateMyProfileRequest
        {
            PhoneNumber = "05330001122", Iban = "TR130006200000000000000002", IbanHolderName = "Ahmet Öztürk", ConfirmOwnIban = true
        }));
        Assert.True(otherName.Errors.ContainsKey("IbanHolderName"));

        var notConfirmed = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.UpdateMyProfileAsync(new UpdateMyProfileRequest
        {
            PhoneNumber = "05330001122", Iban = "TR130006200000000000000002", IbanHolderName = "Mehmet Öztürk", ConfirmOwnIban = false
        }));
        Assert.True(notConfirmed.Errors.ContainsKey("ConfirmOwnIban"));
    }

    [Theory]
    [InlineData("TR240015700000000000000101")]   // Enpara Bank (00157)
    [InlineData("TR590082900000000000000101")]   // Papara elektronik para (00829)
    public async Task UpdateMyProfile_RejectsDigitalBanksAndEMoneyAccounts(string iban)
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using var scope = db.NewScope();

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.UpdateMyProfileAsync(new UpdateMyProfileRequest
        {
            PhoneNumber = "05330001122", Iban = WithValidCheckDigits(iban), IbanHolderName = "Mehmet Öztürk", ConfirmOwnIban = true
        }));
        Assert.True(ex.Errors.ContainsKey("Iban"));
    }

    /// <summary>Verilen IBAN'ın kontrol hanelerini (3-4. haneler) mod-97 kuralına göre düzeltir.</summary>
    private static string WithValidCheckDigits(string iban)
    {
        var account = iban[4..];
        var digits = account + "2927" + "00";        // T=29, R=27
        var remainder = digits.Aggregate(0, (r, c) => (r * 10 + (c - '0')) % 97);
        return $"TR{98 - remainder:00}{account}";
    }
}

public class StudentManagementTests
{
    private static async Task<int> AddAdvisorAsync(TestDb db, int departmentId, string registry, string name)
    {
        using var ctx = db.NewContext();
        var user = new IskurAuto.Core.Entities.User
        {
            FullName = name, RegistryNumber = registry, Email = $"{registry}@test.local",
            PasswordHash = db.Hasher.Hash(TestData.Password), Role = UserRole.Advisor, DepartmentId = departmentId
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<int> AddStudentAsync(TestDb db, int departmentId, string number, string nationalId)
    {
        using var ctx = db.NewContext();
        var profile = new IskurAuto.Core.Entities.StudentProfile
        {
            User = new IskurAuto.Core.Entities.User { FullName = $"Öğrenci {number}", Email = $"{number}@ogr.test", PasswordHash = "x", Role = UserRole.Student },
            NationalId = nationalId, Iban = "TR400006200000000000000001", StudentNumber = number, DepartmentId = departmentId, RegistrationYear = 2025
        };
        profile.Participations.Add(new IskurAuto.Core.Entities.ProgramParticipation { StartDate = db.Clock.Today.AddDays(-10) });
        ctx.StudentProfiles.Add(profile);
        await ctx.SaveChangesAsync();
        return profile.Id;
    }

    [Fact]
    public async Task AssignAdvisor_ByRegistryNumber()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        await AddAdvisorAsync(db, seed.BmId, "300050", "Ali Veli");
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        var detail = await scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorRegistryNumber = " 300050 " });
        Assert.Equal("Ali Veli", detail.CurrentAdvisor!.AdvisorName);

        // Bilinmeyen ya da sorumlu personel olmayan sicil numarası reddedilir.
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorRegistryNumber = "999999" }));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorRegistryNumber = "100001" }));
    }

    [Fact]
    public async Task BulkAssign_AssignsAllSelectedStudentsInOneOperation_AndReportsTheOnesThatFail()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var second = await AddStudentAsync(db, seed.BmId, "230000009", "10000000146");
        await AddAdvisorAsync(db, seed.BmId, "300050", "Ali Veli");
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            var result = await scope.Students.BulkAssignAsync(new BulkAssignRequest
            {
                StudentIds = new() { seed.MehmetId, second, seed.ElifId },
                WorkUnitId = seed.LibraryId,
                AdvisorRegistryNumber = "300050"
            });

            // Elif başka bölümde: birimi atanır ama BM'nin sorumlu personeli atanamaz → hata listesinde.
            Assert.Equal(2, result.Assigned);
            var error = Assert.Single(result.Errors);
            Assert.Equal(seed.ElifId, error.StudentId);
        }

        using var verify = db.NewContext();
        foreach (var id in new[] { seed.MehmetId, second })
        {
            Assert.True(await verify.StudentUnitAssignments.AnyAsync(a => a.StudentProfileId == id && a.EndDate == null && a.WorkUnitId == seed.LibraryId));
            Assert.True(await verify.AdvisorAssignments.AnyAsync(a => a.StudentProfileId == id && a.EndDate == null && a.AdvisorUser.RegistryNumber == "300050"));
        }
    }

    [Fact]
    public async Task BulkAssign_DepartmentHead_CanAssignAdvisorsButNotUnits()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        await AddAdvisorAsync(db, seed.BmId, "300050", "Ali Veli");
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Students.BulkAssignAsync(new BulkAssignRequest
        {
            StudentIds = new() { seed.MehmetId }, WorkUnitId = seed.LibraryId
        }));

        var result = await scope.Students.BulkAssignAsync(new BulkAssignRequest
        {
            StudentIds = new() { seed.MehmetId, seed.ElifId }, AdvisorRegistryNumber = "300050"
        });
        Assert.Equal(1, result.Assigned);                       // Elif başka bölümde: görünmez
        Assert.Equal(seed.ElifId, Assert.Single(result.Errors).StudentId);
    }

    [Fact]
    public async Task UpdateStudent_ByFaculty_EditsEverything_AndChangingDepartmentClosesTheAdvisorAssignment()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            var updated = await scope.Students.UpdateAsync(seed.MehmetId, new UpdateStudentRequest
            {
                FullName = "Mehmet Can Öztürk", Email = "230000001@ogr.selcuk.edu.tr", PhoneNumber = "05330001122",
                NationalId = "12345678950", StudentNumber = "230000001", DepartmentId = seed.YmId, RegistrationYear = 2023,
                NewObisPassword = "YeniObis1"
            });
            Assert.Equal("Mehmet Can Öztürk", updated.FullName);
            Assert.Equal("Yazılım Mühendisliği", updated.DepartmentName);
            Assert.Null(updated.CurrentAdvisor);
        }

        db.Current.Anonymous();
        using var login = db.NewScope();
        await login.Auth.LoginAsync(new LoginRequest { Username = "230000001@ogr.selcuk.edu.tr", Password = "YeniObis1", Role = UserRole.Student });
    }

    [Fact]
    public async Task UpdateStudent_OnlyFaculty()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Students.UpdateAsync(seed.MehmetId, new UpdateStudentRequest
        {
            FullName = "X", Email = "x@test.local", PhoneNumber = "05330001122", NationalId = "12345678950",
            StudentNumber = "230000001", DepartmentId = seed.BmId, RegistrationYear = 2023
        }));
    }
}

/// <summary>Belge madde 9: "geçmiş kayıtlar kaybolmamalı", "ayrılan öğrencinin kayıtları silinmemeli".</summary>
public class StudentHistoryTests
{
    [Fact]
    public async Task AssignUnit_ClosesPreviousAssignment_AndKeepsHistory()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            var detail = await scope.Students.AssignUnitAsync(seed.MehmetId, new AssignUnitRequest { WorkUnitId = seed.LibraryId });
            Assert.Equal("Kütüphane", detail.CurrentUnit!.WorkUnitName);
            Assert.Equal(2, detail.UnitHistory.Count);                       // eski kayıt silinmedi
            Assert.Single(detail.UnitHistory, u => u.EndDate is null);       // yalnızca biri açık
        }

        // Ayrı bir istekte (taze context) veritabanından doğrula.
        using var verify = db.NewContext();
        var rows = await verify.StudentUnitAssignments.Where(a => a.StudentProfileId == seed.MehmetId).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Single(rows, a => a.EndDate == null);
    }

    [Fact]
    public async Task AssignUnit_ToSameUnit_ThrowsConflict()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ConflictException>(() =>
            scope.Students.AssignUnitAsync(seed.MehmetId, new AssignUnitRequest { WorkUnitId = seed.LabId }));
    }

    [Fact]
    public async Task AssignUnit_OnlyFacultyAdminMayDoIt()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            scope.Students.AssignUnitAsync(seed.MehmetId, new AssignUnitRequest { WorkUnitId = seed.LibraryId }));
    }

    [Fact]
    public async Task AssignAdvisor_ReplacesCurrentAdvisor_AndKeepsHistory()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        // BM'ye ikinci bir danışman ekle.
        int secondAdvisorId;
        using (var ctx = db.NewContext())
        {
            var second = new IskurAuto.Core.Entities.User
            {
                FullName = "Ali Veli", Email = "advisor2@test.local", PasswordHash = db.Hasher.Hash(TestData.Password),
                Role = UserRole.Advisor, DepartmentId = seed.BmId
            };
            ctx.Users.Add(second);
            await ctx.SaveChangesAsync();
            secondAdvisorId = second.Id;
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
        {
            var detail = await scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorUserId = secondAdvisorId });
            Assert.Equal("Ali Veli", detail.CurrentAdvisor!.AdvisorName);
            Assert.Equal(2, detail.AdvisorHistory.Count);
        }

        // Eski danışman artık bu öğrenciyi göremez, yeni danışman görür.
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Students.GetByIdAsync(seed.MehmetId));

        db.Current.As(UserRole.Advisor, secondAdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Students.GetByIdAsync(seed.MehmetId);
    }

    [Fact]
    public async Task AssignAdvisor_Rules()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);

        using var scope = db.NewScope();

        // Başka bölümün danışmanı olamaz.
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorUserId = seed.AdvisorYmId }));
        // Danışman rolünde olmayan kullanıcı olamaz.
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorUserId = seed.AdminId }));
        // Zaten danışmanı olan kişi tekrar atanamaz.
        await Assert.ThrowsAsync<ConflictException>(() =>
            scope.Students.AssignAdvisorAsync(seed.MehmetId, new AssignAdvisorRequest { AdvisorUserId = seed.AdvisorId }));
        // Başka bölümün öğrencisine atama yapamaz (varlığı da sızmaz).
        await Assert.ThrowsAsync<NotFoundException>(() =>
            scope.Students.AssignAdvisorAsync(seed.ElifId, new AssignAdvisorRequest { AdvisorUserId = seed.AdvisorYmId }));
    }

    [Fact]
    public async Task EndParticipation_MakesStudentPassive_KeepsAllRecords_AndClosesAssignments()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
        {
            var detail = await scope.Students.EndParticipationAsync(seed.MehmetId, new EndParticipationRequest { Reason = "Dönem sonu ayrılma" });

            Assert.False(detail.IsActive);
            Assert.Null(detail.CurrentUnit);
            Assert.Null(detail.CurrentAdvisor);
            var participation = Assert.Single(detail.Participations);
            Assert.Equal("Dönem sonu ayrılma", participation.EndReason);
        }

        // Kayıtlar hâlâ veritabanında (silinmedi) ve aktif filtresinde artık görünmüyor.
        using var verify = db.NewContext();
        Assert.True(await verify.StudentProfiles.AnyAsync(s => s.Id == seed.MehmetId));
        Assert.Equal(1, await verify.AdvisorAssignments.CountAsync(a => a.StudentProfileId == seed.MehmetId));

        using (var scope = db.NewScope())
            Assert.Equal(0, (await scope.Students.SearchAsync(new StudentSearchQuery { IsActive = true, Text = "Mehmet" })).TotalCount);
    }

    [Fact]
    public async Task EndParticipation_Rules()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        // Gelecek tarih ve başlangıçtan önceki tarih geçersiz.
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.EndParticipationAsync(seed.MehmetId,
            new EndParticipationRequest { Reason = "gerekçe", EndDate = db.Clock.Today.AddDays(5) }));
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.EndParticipationAsync(seed.MehmetId,
            new EndParticipationRequest { Reason = "gerekçe", EndDate = db.Clock.Today.AddDays(-400) }));

        await scope.Students.EndParticipationAsync(seed.MehmetId, new EndParticipationRequest { Reason = "gerekçe" });

        // Zaten pasifken tekrar sonlandırılamaz; pasife birim/danışman atanamaz.
        await Assert.ThrowsAsync<ConflictException>(() => scope.Students.EndParticipationAsync(seed.MehmetId,
            new EndParticipationRequest { Reason = "gerekçe" }));
        await Assert.ThrowsAsync<ConflictException>(() => scope.Students.AssignUnitAsync(seed.MehmetId,
            new AssignUnitRequest { WorkUnitId = seed.LibraryId }));
    }

    [Fact]
    public async Task StartParticipation_AfterEnd_AddsSecondPeriod_AndKeepsFirst()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
            await scope.Students.EndParticipationAsync(seed.MehmetId, new EndParticipationRequest { Reason = "gerekçe" });

        db.Clock.AdvanceDays(90);
        using (var scope = db.NewScope())
        {
            var detail = await scope.Students.StartParticipationAsync(seed.MehmetId, new StartParticipationRequest());
            Assert.True(detail.IsActive);
            Assert.Equal(2, detail.Participations.Count);                       // geçmiş dönem korunur
            Assert.Single(detail.Participations, p => p.IsOpen);

            // Aktifken ikinci kez başlatılamaz.
            await Assert.ThrowsAsync<ConflictException>(() =>
                scope.Students.StartParticipationAsync(seed.MehmetId, new StartParticipationRequest()));
        }
    }

    [Fact]
    public async Task StartParticipation_CannotOverlapPreviousPeriod()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using var scope = db.NewScope();
        await scope.Students.EndParticipationAsync(seed.MehmetId, new EndParticipationRequest { Reason = "gerekçe" });

        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Students.StartParticipationAsync(
            seed.MehmetId, new StartParticipationRequest { StartDate = db.Clock.Today.AddDays(-10) }));
    }
}
