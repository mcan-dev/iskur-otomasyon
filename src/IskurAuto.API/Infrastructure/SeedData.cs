using IskurAuto.Core.Entities;
using IskurAuto.Data.Context;

namespace IskurAuto.API.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IskurAutoDbContext>();

        // Veritabanı oluşturulmuş mu emin ol (migration'ları uygula)
        await context.Database.EnsureCreatedAsync();

        if (context.Faculties.Any())
            return; // Zaten veri var

        // 1. Rolleri oluştur
        var makerRole = new Role { Name = "FacultySecretary", Description = "Fakülte Sekreteri (Maker)" };
        var checkerRole = new Role { Name = "SksAdmin", Description = "SKS Admin (Checker)" };
        
        context.Roles.AddRange(makerRole, checkerRole);
        await context.SaveChangesAsync();

        // 2. Fakülte oluştur
        var faculty = new Faculty { Name = "Mühendislik Fakültesi", Code = "MF", IsActive = true };
        context.Faculties.Add(faculty);
        await context.SaveChangesAsync();

        // 3. Kullanıcıları oluştur (Maker ve Checker)
        // JWT'de makerUserId = 1, checkerUserId = 2 demiştik
        var makerUser = new User
        {
            FullName = "Ali Yılmaz",
            Email = "ali.maker@iskurauto.edu.tr",
            PasswordHash = "dummyhash",
            RoleId = makerRole.Id,
            FacultyId = faculty.Id, // Kendi fakültesine bağlı
            IsActive = true
        };

        var checkerUser = new User
        {
            FullName = "Ayşe Kaya",
            Email = "ayse.checker@iskurauto.edu.tr",
            PasswordHash = "dummyhash",
            RoleId = checkerRole.Id,
            FacultyId = null, // SKS Admin tüm fakültelere bakar
            IsActive = true
        };

        context.Users.AddRange(makerUser, checkerUser);
        await context.SaveChangesAsync(); // Bu noktada Maker User'ın ID'si 1, Checker User'ın ID'si 2 olacak (temiz DB ise)

        // 4. Öğrenci oluştur (FacultyId = 1)
        var student = new StudentWorker
        {
            FirstName = "Mehmet",
            LastName = "Öztürk",
            NationalId = "33333333330",
            StudentNumber = "2023000001",
            IskurRegistrationNumber = "ISK12345",
            FacultyId = faculty.Id, // Maker'ın fakültesi ile aynı
            IsActive = true
        };

        context.StudentWorkers.Add(student);
        await context.SaveChangesAsync();
    }
}
