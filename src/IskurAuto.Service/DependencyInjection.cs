using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
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
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, WorkPlanSettings? workPlanSettings = null)
    {
        services.AddSingleton(workPlanSettings ?? new WorkPlanSettings());
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher>(new Pbkdf2PasswordHasher());

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IWorkUnitService, WorkUnitService>();
        services.AddScoped<IWorkPlanService, WorkPlanService>();
        services.AddScoped<ICourseDayService, CourseDayService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<LeavePolicyService>();
        services.AddScoped<ILeavePolicyProvider>(sp => sp.GetRequiredService<LeavePolicyService>());
        services.AddScoped<ILeaveSettingsService>(sp => sp.GetRequiredService<LeavePolicyService>());
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IAbsenceService, AbsenceService>();

        return services;
    }
}
