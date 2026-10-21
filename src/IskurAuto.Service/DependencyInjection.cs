using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.Implementations;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Security;
using Microsoft.Extensions.DependencyInjection;

namespace IskurAuto.Service;

/// <summary>Service katmanının bağımlılık kayıtları. Program.cs yalnızca <c>AddServiceLayer()</c> der.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher>(new Pbkdf2PasswordHasher());

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IWorkUnitService, WorkUnitService>();

        return services;
    }
}
