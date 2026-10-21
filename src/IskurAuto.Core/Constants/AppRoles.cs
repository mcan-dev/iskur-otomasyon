namespace IskurAuto.Core.Constants;

/// <summary>
/// [Authorize(Roles = ...)] ve JWT "role" claim'inde kullanılan rol adları.
/// Değerler <see cref="Enums.UserRole"/> üyelerinin adlarıyla birebir aynıdır;
/// böylece string literal tekrarı ve yazım hatası riski ortadan kalkar.
/// </summary>
public static class AppRoles
{
    public const string FacultyAdmin   = nameof(Enums.UserRole.FacultyAdmin);
    public const string DepartmentHead = nameof(Enums.UserRole.DepartmentHead);
    public const string Advisor        = nameof(Enums.UserRole.Advisor);
    public const string Student        = nameof(Enums.UserRole.Student);

    /// <summary>Öğrenci dışındaki, personel sayılan roller (virgülle ayrılmış: Authorize için).</summary>
    public const string AllStaff = FacultyAdmin + "," + DepartmentHead + "," + Advisor;
}
