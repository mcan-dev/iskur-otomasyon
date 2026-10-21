using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using IskurAuto.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IskurAuto.Data;

/// <summary>
/// Data katmanının tüm bağımlılık kayıtları tek yerde. API'nin Program.cs'i yalnızca
/// <c>AddDataLayer(...)</c> der; hangi repository'nin hangi arayüze bağlandığını bilmek zorunda kalmaz.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IskurAutoDbContext>(options => options.UseSqlite(connectionString));

        // DbContext aynı zamanda IUnitOfWork'tür; aynı scoped örnek paylaşılır.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IskurAutoDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IWorkUnitRepository, WorkUnitRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    /// <summary>
    /// Veritabanını hazırlar. Migration dosyaları varsa onları uygular; henüz üretilmemişse
    /// şemayı doğrudan oluşturur (ilk geliştirme aşaması için kolaylık).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IskurAutoDbContext>();

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);
    }
}
