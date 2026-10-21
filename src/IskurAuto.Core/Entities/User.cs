using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>
/// Sisteme giriş yapabilen tüm kişiler (personel ve öğrenciler).
/// Öğrenciye özgü bilgiler (TC, IBAN...) burada değil <see cref="StudentProfile"/> içindedir:
/// böylece kimlik doğrulama verisi ile kişisel veri birbirinden ayrılır (KVKK: veri minimizasyonu).
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Küçük harfe normalize edilmiş e-posta. Giriş adı olarak kullanılır, benzersizdir.</summary>
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    /// <summary>PBKDF2 ile üretilmiş parola özeti. Parola asla açık metin saklanmaz (belge madde 11).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>
    /// Bölüm Başkanı ve Danışman için zorunlu (yetki sınırı). Fakülte yönetimi için null.
    /// Öğrencinin bölümü <see cref="StudentProfile.DepartmentId"/> alanındadır; aynı bilgiyi iki yerde
    /// tutmayarak tutarsızlık riskini baştan engelliyoruz.
    /// </summary>
    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public Department? Department { get; set; }
    public StudentProfile? StudentProfile { get; set; }
}
