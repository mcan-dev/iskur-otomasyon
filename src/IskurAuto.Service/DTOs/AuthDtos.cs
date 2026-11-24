using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

// DTO = Data Transfer Object. Dışarıya açılan veri şekli, veritabanı entity'sinden bilerek ayrıdır:
// böylece PasswordHash gibi alanlar yanlışlıkla API yanıtına sızamaz ve tablo yapısı değişse bile
// API sözleşmesi (frontend'in gördüğü şekil) sabit kalabilir.
// DataAnnotations yalnızca "şekil" kontrolüdür (boş mu, uzunluk); anlamsal kurallar (TC algoritması,
// benzersizlik...) servis katmanındadır.

/// <summary>
/// Giriş. Öğrenci üniversite öğrenci e-postası ve OBİS şifresiyle; personel sicil numarası ve şifresiyle girer.
/// Bu bilgiler fakültedeki İŞKUR öğrencileri için bilgi işlemden alınır; sistemde kayıt olma ekranı yoktur.
/// </summary>
public class LoginRequest
{
    /// <summary>Öğrenci için öğrenci e-postası, personel için sicil numarası.</summary>
    [Required, StringLength(254)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Öğrenci için OBİS şifresi, personel için personel şifresi.</summary>
    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Giriş sayfasının türü; hesabın rolü bununla eşleşmelidir (her rolün kendi giriş sayfası vardır).</summary>
    public UserRole Role { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserDto User { get; set; } = null!;
}

public class ChangePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required, StringLength(254)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required, StringLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Personel sicil numarası; öğrencide boş.</summary>
    public string? RegistryNumber { get; set; }

    public string? PhoneNumber { get; set; }

    /// <summary>FacultyAdmin | DepartmentHead (birim yetkilisi) | Advisor (sorumlu personel) | Student</summary>
    public string Role { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
}
