using IskurAuto.Service.Common;
using IskurAuto.Service.Security;
using IskurAuto.Service.Validation;
using Xunit;

namespace IskurAuto.Tests.Unit;

// Bu sınıftaki testler veritabanına dokunmaz: saf mantık, milisaniyede çalışır.

public class NationalIdValidatorTests
{
    [Theory]
    [InlineData("12345678950")]
    [InlineData("23456789138")]
    [InlineData("34567891238")]
    public void IsValid_ForCorrectNumbers_ReturnsTrue(string value)
        => Assert.True(NationalIdValidator.IsValid(value));

    [Theory]
    [InlineData("12345678901")]   // kontrol hanesi hatalı
    [InlineData("02345678950")]   // ilk hane 0
    [InlineData("1234567895")]    // 10 hane
    [InlineData("123456789500")]  // 12 hane
    [InlineData("1234567895a")]   // rakam olmayan karakter
    [InlineData("")]
    public void IsValid_ForWrongNumbers_ReturnsFalse(string value)
        => Assert.False(NationalIdValidator.IsValid(value));

    [Fact]
    public void IsValid_ForNull_ReturnsFalse() => Assert.False(NationalIdValidator.IsValid(null));
}

public class IbanValidatorTests
{
    [Theory]
    [InlineData("TR400006200000000000000001")]
    [InlineData("TR130006200000000000000002")]
    [InlineData("TR830006200000000000000003")]
    public void IsValid_ForCorrectIban_ReturnsTrue(string value)
        => Assert.True(IbanValidator.IsValid(value));

    [Theory]
    [InlineData("TR400006200000000000000002")]   // kontrol hanesi uymuyor
    [InlineData("TR4000062000000000000000")]     // kısa
    [InlineData("DE89370400440532013000")]       // Türkiye IBAN'ı değil
    [InlineData("TR40000620000000000000000A")]   // harf içeriyor
    [InlineData("")]
    public void IsValid_ForWrongIban_ReturnsFalse(string value)
        => Assert.False(IbanValidator.IsValid(value));

    [Fact]
    public void Normalize_RemovesSpacesAndUppercases()
        => Assert.Equal("TR400006200000000000000001", IbanValidator.Normalize(" tr40 0006 2000 0000 0000 0000 01 "));
}

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Parola123")]
    [InlineData("abc12345")]
    public void Validate_ForStrongPassword_ReturnsNull(string value)
        => Assert.Null(PasswordPolicy.Validate(value));

    [Theory]
    [InlineData("")]
    [InlineData("a1")]            // kısa
    [InlineData("12345678")]      // harf yok
    [InlineData("parolaparola")]  // rakam yok
    public void Validate_ForWeakPassword_ReturnsMessage(string value)
        => Assert.NotNull(PasswordPolicy.Validate(value));
}

public class InputNormalizerTests
{
    [Theory]
    [InlineData("0532 123 45 67", "05321234567")]
    [InlineData("+90 532 123 45 67", "05321234567")]
    [InlineData("(0532) 123-4567", "05321234567")]
    [InlineData("5321234567", "05321234567")]
    public void Phone_NormalizesToZeroFiveFormat(string input, string expected)
        => Assert.Equal(expected, InputNormalizer.Phone(input));

    [Theory]
    [InlineData("05321234567", true)]
    [InlineData("02121234567", false)]   // sabit hat
    [InlineData("0532123456", false)]    // kısa
    public void IsValidMobile_ChecksFormat(string input, bool expected)
        => Assert.Equal(expected, InputNormalizer.IsValidMobile(input));

    [Fact]
    public void Email_TrimsAndLowercases() => Assert.Equal("ali@x.com", InputNormalizer.Email("  Ali@X.com "));

    [Fact]
    public void Name_CollapsesWhitespace() => Assert.Equal("Ali Veli", InputNormalizer.Name("  Ali    Veli "));
}

public class PersonalDataMaskerTests
{
    [Fact]
    public void MaskNationalId_KeepsFirstThreeAndLastTwo()
        => Assert.Equal("123******50", PersonalDataMasker.MaskNationalId("12345678950"));

    [Fact]
    public void MaskIban_KeepsPrefixAndLastTwo()
    {
        var masked = PersonalDataMasker.MaskIban("TR400006200000000000000001");
        Assert.Equal(26, masked.Length);
        Assert.StartsWith("TR40", masked);
        Assert.EndsWith("01", masked);
        Assert.DoesNotContain("0006200", masked);
    }
}

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1_000);

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
        => Assert.True(_hasher.Verify(_hasher.Hash("Parola123"), "Parola123"));

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
        => Assert.False(_hasher.Verify(_hasher.Hash("Parola123"), "Parola124"));

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        => Assert.NotEqual(_hasher.Hash("Parola123"), _hasher.Hash("Parola123"));   // rastgele salt

    [Fact]
    public void Hash_DoesNotContainPlainPassword()
        => Assert.DoesNotContain("Parola123", _hasher.Hash("Parola123"));

    [Theory]
    [InlineData("")]
    [InlineData("bozuk")]
    [InlineData("v1.abc.def.ghi")]
    [InlineData("v2.1000.AAAA.BBBB")]
    public void Verify_WithMalformedHash_ReturnsFalseInsteadOfThrowing(string hash)
        => Assert.False(_hasher.Verify(hash, "Parola123"));

    [Fact]
    public void Verify_StillWorksWhenIterationCountChangesLater()
    {
        var oldHash = new Pbkdf2PasswordHasher(iterations: 1_000).Hash("Parola123");
        Assert.True(new Pbkdf2PasswordHasher(iterations: 2_000).Verify(oldHash, "Parola123"));
    }
}
