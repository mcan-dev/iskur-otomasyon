namespace IskurAuto.API.Infrastructure;

/// <summary>
/// JWT içinde kullanılan özel claim adları. Token üretimi (JwtTokenService) ve okuma (CurrentUser)
/// aynı sabitleri kullanır; string literal tekrarı ve yazım hatası riskini önler.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>Kullanıcının veritabanı Id'si.</summary>
    public const string UserId = "uid";

    /// <summary>Kullanıcının rolü: FacultyAdmin | DepartmentHead | Advisor | Student.</summary>
    public const string Role = "role";

    /// <summary>Bölüm Başkanı / Danışman için bölüm Id'si (yetki sınırı).</summary>
    public const string DepartmentId = "did";

    public const string Email = "email";
}
