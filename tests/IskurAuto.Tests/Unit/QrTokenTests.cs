using IskurAuto.Service.Attendance;
using Xunit;

namespace IskurAuto.Tests.Unit;

public class QrTokenTests
{
    private static readonly string Secret = QrToken.NewSecret();

    // 30 saniyelik pencerenin tam başlangıcı: sınır testleri bu noktaya göre yazılır.
    private static readonly DateTime WindowStart = DateTime.UnixEpoch.AddSeconds(30 * 60_000);

    [Fact]
    public void FreshToken_IsValid()
    {
        var token = QrToken.Create(7, Secret, WindowStart.AddSeconds(5));
        Assert.True(QrToken.IsValid(token, 7, Secret, WindowStart.AddSeconds(20)));
    }

    [Theory]
    [InlineData(59, true)]    // bir önceki pencerenin jetonu, tolerans içinde
    [InlineData(60, false)]   // iki pencere sonrası geçersiz
    public void Token_ExpiresAfterGraceWindow(int secondsLater, bool expected)
    {
        var token = QrToken.Create(7, Secret, WindowStart);
        Assert.Equal(expected, QrToken.IsValid(token, 7, Secret, WindowStart.AddSeconds(secondsLater)));
    }

    [Fact]
    public void TokenFromTheFuture_IsRejected()
    {
        var token = QrToken.Create(7, Secret, WindowStart.AddSeconds(31));
        Assert.False(QrToken.IsValid(token, 7, Secret, WindowStart.AddSeconds(5)));
    }

    [Fact]
    public void Token_IsBoundToSessionAndSecret()
    {
        var token = QrToken.Create(7, Secret, WindowStart);

        Assert.False(QrToken.IsValid(token, 8, Secret, WindowStart));
        Assert.False(QrToken.IsValid(token, 7, QrToken.NewSecret(), WindowStart));
    }

    [Fact]
    public void TamperedToken_IsRejected()
    {
        var token = QrToken.Create(7, Secret, WindowStart);
        var parts = token.Split('.');

        var otherWindow = $"{parts[0]}.{long.Parse(parts[1]) + 1}.{parts[2]}";
        var otherSignature = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";

        Assert.False(QrToken.IsValid(otherWindow, 7, Secret, WindowStart.AddSeconds(31)));
        Assert.False(QrToken.IsValid(otherSignature, 7, Secret, WindowStart));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("7.x.sig")]
    [InlineData("7.1")]
    [InlineData("7.1.sig.extra")]
    public void MalformedToken_IsRejectedWithoutThrowing(string? token)
    {
        Assert.False(QrToken.IsValid(token, 7, Secret, WindowStart));
    }

    [Fact]
    public void NullToken_IsRejected()
    {
        Assert.False(QrToken.IsValid(null, 7, Secret, WindowStart));
        Assert.False(QrToken.TryReadSessionId(null, out _));
    }

    [Fact]
    public void TokenChangesEveryWindow_AndIsStableInsideOne()
    {
        var first = QrToken.Create(7, Secret, WindowStart);
        var sameWindow = QrToken.Create(7, Secret, WindowStart.AddSeconds(29));
        var nextWindow = QrToken.Create(7, Secret, WindowStart.AddSeconds(30));

        Assert.Equal(first, sameWindow);
        Assert.NotEqual(first, nextWindow);
    }

    [Fact]
    public void RefreshAt_IsTheEndOfTheCurrentWindow()
        => Assert.Equal(WindowStart.AddSeconds(30), QrToken.RefreshAt(WindowStart.AddSeconds(12)));

    [Fact]
    public void TryReadSessionId_ReadsTheFirstPart()
    {
        Assert.True(QrToken.TryReadSessionId("42.100.sig", out var id));
        Assert.Equal(42, id);
        Assert.False(QrToken.TryReadSessionId("0.100.sig", out _));
    }
}
