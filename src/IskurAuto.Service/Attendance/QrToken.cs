using System.Security.Cryptography;
using System.Text;

namespace IskurAuto.Service.Attendance;

/// <summary>
/// Karekodun içindeki kısa ömürlü imzalı jeton: "{oturum}.{zaman penceresi}.{imza}".
/// İmza, oturuma özel gizli anahtarla HMAC-SHA256 ile üretilir; pencere numarası zamandan hesaplandığı
/// için sunucu hiçbir jetonu saklamaz. Saf (durumsuz) olduğundan saat verilerek test edilir.
/// </summary>
public static class QrToken
{
    public const int WindowSeconds = 30;

    /// <summary>Okutma gecikmesine tolerans: bir önceki pencerenin jetonu da kabul edilir.</summary>
    private const int GraceWindows = 1;

    private const int SignatureBytes = 16;

    public static string NewSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Create(int sessionId, string secret, DateTime utcNow)
    {
        var window = WindowOf(utcNow);
        return $"{sessionId}.{window}.{Sign(sessionId, window, secret)}";
    }

    /// <summary>Bu jetonun ekranda gösterilmesi gereken son an; sonrasında yenisi alınmalıdır.</summary>
    public static DateTime RefreshAt(DateTime utcNow)
        => DateTime.UnixEpoch.AddSeconds((WindowOf(utcNow) + 1) * WindowSeconds);

    /// <summary>İmzayı doğrulamadan, jetonun hangi oturuma ait olduğunu okur.</summary>
    public static bool TryReadSessionId(string? token, out int sessionId)
    {
        sessionId = 0;
        var parts = token?.Trim().Split('.');
        return parts is { Length: 3 } && int.TryParse(parts[0], out sessionId) && sessionId > 0;
    }

    public static bool IsValid(string? token, int sessionId, string secret, DateTime utcNow)
    {
        var parts = token?.Trim().Split('.');
        if (parts is not { Length: 3 }
            || !int.TryParse(parts[0], out var tokenSession) || tokenSession != sessionId
            || !long.TryParse(parts[1], out var window))
            return false;

        var current = WindowOf(utcNow);
        if (window > current || window < current - GraceWindows)
            return false;

        var expected = Encoding.ASCII.GetBytes(Sign(sessionId, window, secret));
        var actual = Encoding.ASCII.GetBytes(parts[2]);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static long WindowOf(DateTime utc)
        => (long)(utc - DateTime.UnixEpoch).TotalSeconds / WindowSeconds;

    private static string Sign(int sessionId, long window, string secret)
    {
        var key = Convert.FromBase64String(secret);
        var hash = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes($"{sessionId}.{window}"));
        return Convert.ToBase64String(hash, 0, SignatureBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
