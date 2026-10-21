using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.Abstractions;

namespace IskurAuto.Service.Common;

public static class StudentAccess
{
    /// <summary>Öğrencinin kaydı, isteği yapanın rol kapsamında mı?</summary>
    public static bool CanView(this ICurrentUser user, StudentProfile student) => user.Role switch
    {
        UserRole.FacultyAdmin => true,
        UserRole.DepartmentHead => user.DepartmentId == student.DepartmentId,
        UserRole.Advisor => student.CurrentAdvisorAssignment?.AdvisorUserId == user.UserId,
        UserRole.Student => student.UserId == user.UserId,
        _ => false
    };

    /// <summary>Kapsam dışıysa 403 yerine 404 verir; kaydın varlığı sızmaz.</summary>
    public static void EnsureCanView(this ICurrentUser user, StudentProfile student)
    {
        if (!user.CanView(student))
            throw new NotFoundException("Öğrenci bulunamadı.");
    }
}
