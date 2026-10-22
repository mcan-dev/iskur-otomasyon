using System.ComponentModel.DataAnnotations;

namespace IskurAuto.Service.DTOs;

// DTO = Data Transfer Object. Dışarıya açılan veri şekli, veritabanı entity'sinden bilerek ayrıdır:
// böylece PasswordHash gibi alanlar yanlışlıkla API yanıtına sızamaz ve tablo yapısı değişse bile
// API sözleşmesi (frontend'in gördüğü şekil) sabit kalabilir.
// DataAnnotations yalnızca "şekil" kontrolüdür (boş mu, uzunluk); anlamsal kurallar (TC algoritması,
// benzersizlik...) servis katmanındadır.

public class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
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

/// <summary>Öğrenci ilk kayıt formu (belge madde 3).</summary>
public class StudentRegistrationRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Cep telefonu. Örn: 0532 123 45 67</summary>
    [Required, StringLength(25)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(11, MinimumLength = 11)]
    public string NationalId { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Iban { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 3)]
    public string StudentNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Range(2000, 2100)]
    public int RegistrationYear { get; set; }
}

public class RegistrationResultDto
{
    public int UserId { get; set; }
    public int StudentId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>FacultyAdmin | DepartmentHead | Advisor | Student</summary>
    public string Role { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
}
