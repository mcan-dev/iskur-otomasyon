using System.Security.Cryptography;
using IskurAuto.Service.Abstractions;

namespace IskurAuto.Service.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 ile parola özetleme.
/// Neden PBKDF2: .NET'in içinde hazır gelir (ek paket gerekmez), standarttır ve kasıtlı olarak yavaştır;
/// sızan bir veritabanından parolaları kaba kuvvetle çözmeyi çok pahalı kılar.
/// Neden SHA256 ya da MD5 değil: onlar çok hızlıdır, saldırgan saniyede milyarlarca deneme yapar.
/// Her parola için rastgele "salt" kullanılır; aynı parolanın iki kullanıcıda aynı özeti vermesi engellenir.
/// Saklama biçimi: v1.{yineleme}.{salt-base64}.{özet-base64} — yineleme sayısı özetin içinde saklandığından
/// ileride artırılsa bile eski parolalar doğrulanmaya devam eder.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Version = "v1";
    private const int SaltSize = 16;   // 128 bit
    private const int KeySize = 32;    // 256 bit
    public const int DefaultIterations = 600_000;   // OWASP 2023 önerisi (PBKDF2-HMAC-SHA256)

    private static readonly Lazy<string> Dummy =
        new(() => HashCore("dummy-parola-yalnizca-sure-esitleme-icin", DefaultIterations));

    private readonly int _iterations;

    /// <param name="iterations">Testlerde süreyi kısaltmak için düşürülebilir; üretimde varsayılan kullanılır.</param>
    public Pbkdf2PasswordHasher(int iterations = DefaultIterations) => _iterations = iterations;

    public string DummyHash => Dummy.Value;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return HashCore(password, _iterations);
    }

    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash) || password is null) return false;

        var parts = passwordHash.Split('.');
        if (parts.Length != 4 || parts[0] != Version || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            // Sabit zamanlı karşılaştırma: eşleşen bayt sayısına bakarak parola tahmin edilemesin.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashCore(string password, int iterations)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Version}.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }
}
