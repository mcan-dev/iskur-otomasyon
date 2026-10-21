using System.Text.RegularExpressions;

namespace IskurAuto.Service.Validation;

/// <summary>
/// T.C. Kimlik No doğrulama. Rastgele 11 haneli sayı geçerli değildir; resmi algoritma uygulanır:
///  - 11 haneli, ilk hane 0 olamaz
///  - 10. hane = ((1,3,5,7,9. hanelerin toplamı × 7) − (2,4,6,8. hanelerin toplamı)) mod 10
///  - 11. hane = ilk 10 hanenin toplamı mod 10
/// Neden: yazım hatalarını ve uydurma numaraları kayıt anında yakalar (belge: "veri doğrulama kontrolleri").
/// Not: Bu yalnızca biçim/algoritma kontrolüdür; kişinin gerçekten var olduğunu doğrulamaz (NVİ servisi gerekir).
/// </summary>
public static class NationalIdValidator
{
    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != 11 || !value.All(char.IsAsciiDigit) || value[0] == '0')
            return false;

        var d = value.Select(c => c - '0').ToArray();

        var oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
        var evenSum = d[1] + d[3] + d[5] + d[7];
        var tenth = ((oddSum * 7) - evenSum) % 10;
        if (tenth < 0) tenth += 10;           // C#'ta % negatif sonuç verebilir
        if (d[9] != tenth) return false;

        return d.Take(10).Sum() % 10 == d[10];
    }
}

/// <summary>
/// Türkiye IBAN doğrulaması: "TR" + 2 kontrol hanesi + 22 hane (toplam 26).
/// ISO 13616 mod-97 kuralı: ilk 4 karakter sona alınır, harfler sayıya çevrilir (A=10 ... Z=35),
/// ortaya çıkan büyük sayının 97'ye bölümünden kalan 1 olmalıdır.
/// </summary>
public static class IbanValidator
{
    private static readonly Regex Shape = new("^TR[0-9]{24}$", RegexOptions.Compiled);

    /// <summary>Boşlukları siler ve büyük harfe çevirir ("tr33 0006 ..." → "TR330006...").</summary>
    public static string Normalize(string? value)
        => new string((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    /// <param name="normalized"><see cref="Normalize"/> çıktısı.</param>
    public static bool IsValid(string? normalized)
    {
        if (normalized is null || !Shape.IsMatch(normalized)) return false;

        var rearranged = normalized[4..] + normalized[..4];

        // Büyük sayıyı BigInteger'sız hesapla: her adımda mod 97 al.
        var remainder = 0;
        foreach (var ch in rearranged)
        {
            if (char.IsAsciiDigit(ch))
            {
                remainder = (remainder * 10 + (ch - '0')) % 97;
            }
            else
            {
                var value = ch - 'A' + 10;                       // 10..35 (iki hane)
                remainder = (remainder * 100 + value) % 97;
            }
        }
        return remainder == 1;
    }
}

/// <summary>
/// Parola kuralı: en az 8 karakter, en az bir harf ve bir rakam.
/// Neden uzunluk odaklı: modern öneri (NIST) karmaşık sembol zorlamak yerine yeterli uzunluktur.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <returns>Geçerliyse null, değilse kullanıcıya gösterilecek hata mesajı.</returns>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password)) return "Parola boş olamaz.";
        if (password.Length < MinLength) return $"Parola en az {MinLength} karakter olmalıdır.";
        if (password.Length > MaxLength) return $"Parola en fazla {MaxLength} karakter olabilir.";
        if (!password.Any(char.IsLetter)) return "Parola en az bir harf içermelidir.";
        if (!password.Any(char.IsDigit)) return "Parola en az bir rakam içermelidir.";
        return null;
    }
}

/// <summary>Girdileri kaydetmeden önce tek biçime getirir (normalizasyon).</summary>
public static class InputNormalizer
{
    private static readonly Regex MobilePattern = new("^05[0-9]{9}$", RegexOptions.Compiled);

    /// <summary>E-posta: kırp + küçük harf. "Ali@X.com " ile "ali@x.com" aynı kullanıcıdır.</summary>
    public static string Email(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Ad-soyad: kırp ve art arda boşlukları tek boşluğa indir.</summary>
    public static string Name(string? value)
        => Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");

    /// <summary>
    /// Cep telefonunu "05XXXXXXXXX" biçimine getirir. "+90 532 123 45 67", "0532-123-4567",
    /// "5321234567" hepsi aynı sonucu verir. Tanınmayan biçimde girdiyi olduğu gibi (rakamlara indirgenmiş) döner.
    /// </summary>
    public static string Phone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.StartsWith("90") && digits.Length == 12) digits = "0" + digits[2..];
        else if (digits.Length == 10 && digits[0] == '5') digits = "0" + digits;
        return digits;
    }

    public static bool IsValidMobile(string normalizedPhone) => MobilePattern.IsMatch(normalizedPhone);
}
