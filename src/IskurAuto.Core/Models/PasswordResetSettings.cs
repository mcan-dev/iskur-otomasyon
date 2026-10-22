namespace IskurAuto.Core.Models;

public class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";

    /// <summary>Sıfırlama e-postasındaki bağlantının başlangıcı (arayüzün adresi).</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Bağlantının geçerlilik süresi.</summary>
    public int TokenMinutes { get; set; } = 30;
}
