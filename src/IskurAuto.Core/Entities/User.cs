namespace IskurAuto.Core.Entities;

/// <summary>
/// Sistemi kullanan kullanıcı entity'si.
/// Fakülte Sekreteri (Maker) veya SKS Admin (Checker) rolünde olabilir.
/// FacultyId alanı ile Multi-Tenant izolasyonu sağlanır.
/// SKS Admin rolündeki kullanıcıların FacultyId'si null olabilir (tüm fakülteleri yönetir).
/// </summary>
public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Şifre hash'i (plain text saklanmaz).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int RoleId { get; set; }

    /// <summary>
    /// Fakülte Sekreteri için bağlı olduğu fakülteyi gösterir.
    /// SKS Admin için null olabilir.
    /// </summary>
    public int? FacultyId { get; set; }

    // Navigation Properties
    public Role Role { get; set; } = null!;
    public Faculty? Faculty { get; set; }
}
