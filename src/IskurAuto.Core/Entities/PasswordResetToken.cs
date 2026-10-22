namespace IskurAuto.Core.Entities;

/// <summary>
/// Parola sıfırlama bağlantısı. Jetonun kendisi saklanmaz, yalnızca özeti tutulur;
/// veritabanı sızsa bile bağlantı geri kurulamaz. Jeton tek kullanımlıktır.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Kullanıldıysa veya yeni bir istek açılınca geçersiz kılındıysa dolar.</summary>
    public DateTime? UsedAt { get; set; }

    public User User { get; set; } = null!;
}
