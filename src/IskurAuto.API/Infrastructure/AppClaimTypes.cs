namespace IskurAuto.API.Infrastructure;

/// <summary>
/// JWT token içinde kullanılan özel claim isimlerini merkezi olarak tanımlar.
/// Hem token üretimi hem de controller tarafındaki claim okuma işlemlerinde
/// bu sabitler kullanılmalıdır; string literal tekrarının önüne geçer.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>Kullanıcının veritabanı ID'si.</summary>
    public const string UserId = "uid";

    /// <summary>Kullanıcının bağlı olduğu fakültenin ID'si (Tenant kimliği).</summary>
    public const string FacultyId = "fid";

    /// <summary>Kullanıcının rolü (Örn: "FacultySecretary", "SksAdmin").</summary>
    public const string Role = "role";
}
