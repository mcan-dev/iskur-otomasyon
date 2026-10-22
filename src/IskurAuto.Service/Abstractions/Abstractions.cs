using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.Abstractions;

/// <summary>
/// İsteği yapan kullanıcının kimliği. Servis katmanı HTTP'yi (HttpContext, claim) bilmez;
/// bu arayüzü API katmanı JWT claim'lerinden doldurur. Testlerde kolayca taklit edilir.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Kimliği doğrulanmamışsa 0.</summary>
    int UserId { get; }

    /// <summary>Kimliği doğrulanmamışsa null.</summary>
    UserRole? Role { get; }

    /// <summary>Bölüm Başkanı / Danışman için bölüm Id'si (token'daki değer).</summary>
    int? DepartmentId { get; }

    string? Email { get; }

    string? IpAddress { get; }
}

/// <summary>Parola özetleme. Uygulama: PBKDF2 (bkz. Pbkdf2PasswordHasher).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);

    /// <summary>
    /// Geçerli biçimde, ama hiçbir gerçek parolaya ait olmayan bir özet.
    /// Kullanıcı bulunamayınca bunun üzerinde de doğrulama yapılır; böylece yanıt süresine
    /// bakarak "bu e-posta kayıtlı mı?" sorusunun cevabı sızdırılamaz (kullanıcı numaralandırma saldırısı).
    /// </summary>
    string DummyHash { get; }
}

/// <summary>
/// Erişim jetonu üretici. Arayüz burada, JWT uygulaması API katmanında:
/// jeton biçimi bir sunum/altyapı ayrıntısıdır, iş mantığı değildir.
/// </summary>
public interface ITokenService
{
    TokenResult CreateToken(User user);
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);

/// <summary>
/// E-posta gönderimi. Uygulamalar hata fırlatmaz (hata kaydedilir); aksi halde "parolamı unuttum"
/// isteğinin cevabı, e-postanın kayıtlı olup olmadığını belli ederdi.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
