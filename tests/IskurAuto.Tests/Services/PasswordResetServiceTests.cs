using IskurAuto.Core.Constants;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class PasswordResetServiceTests
{
    private const string NewPassword = "YeniParola123";

    private static async Task<string> RequestTokenAsync(TestDb db, string email = "admin@test.local")
    {
        using var scope = db.NewScope();
        await scope.PasswordReset.RequestAsync(new ForgotPasswordRequest { Email = email });
        return scope.Email.LastToken();
    }

    [Fact]
    public async Task Request_ForKnownEmail_SendsLinkAndStoresOnlyTheHash()
    {
        using var db = new TestDb();
        TestData.Seed(db);

        string token;
        using (var scope = db.NewScope())
        {
            await scope.PasswordReset.RequestAsync(new ForgotPasswordRequest { Email = " ADMIN@test.local " });
            var mail = Assert.Single(scope.Email.Sent);
            Assert.Equal("admin@test.local", mail.To);
            Assert.Contains("/sifre-sifirla?token=", mail.Body);
            token = scope.Email.LastToken();
        }

        using var verify = db.NewContext();
        var stored = await verify.PasswordResetTokens.SingleAsync();
        Assert.NotEqual(token, stored.TokenHash);
        Assert.True(stored.ExpiresAt > db.Clock.UtcNow);
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.PasswordResetRequested));
    }

    [Fact]
    public async Task Request_ForUnknownOrInactiveEmail_DoesNothingAndDoesNotFail()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        using (var scope = db.NewScope())
        {
            await scope.PasswordReset.RequestAsync(new ForgotPasswordRequest { Email = "yok@test.local" });
            Assert.Empty(scope.Email.Sent);
        }

        using (var ctx = db.NewContext())
        {
            var user = await ctx.Users.SingleAsync(u => u.Email == "admin@test.local");
            user.IsActive = false;
            await ctx.SaveChangesAsync();
        }
        using (var scope = db.NewScope())
        {
            await scope.PasswordReset.RequestAsync(new ForgotPasswordRequest { Email = "admin@test.local" });
            Assert.Empty(scope.Email.Sent);
        }

        using var verify = db.NewContext();
        Assert.False(await verify.PasswordResetTokens.AnyAsync());
        Assert.NotEqual(0, seed.BmId);
    }

    [Fact]
    public async Task Reset_WithValidToken_ChangesPasswordSoNewOneLogsIn()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        var token = await RequestTokenAsync(db);

        using (var scope = db.NewScope())
            await scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = token, NewPassword = NewPassword });

        using var login = db.NewScope();
        var ok = await login.Auth.LoginAsync(new LoginRequest { Email = "admin@test.local", Password = NewPassword });
        Assert.False(string.IsNullOrEmpty(ok.AccessToken));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            login.Auth.LoginAsync(new LoginRequest { Email = "admin@test.local", Password = TestData.Password }));

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.PasswordResetCompleted));
    }

    [Fact]
    public async Task Reset_Token_CanBeUsedOnlyOnce()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        var token = await RequestTokenAsync(db);

        using (var scope = db.NewScope())
            await scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = token, NewPassword = NewPassword });

        using var again = db.NewScope();
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            again.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = token, NewPassword = "BaskaParola123" }));
        Assert.True(ex.Errors.ContainsKey("Token"));
    }

    [Fact]
    public async Task Reset_AfterExpiry_IsRejected()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        var token = await RequestTokenAsync(db);
        db.Clock.AdvanceMinutes(31);

        using var scope = db.NewScope();
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = token, NewPassword = NewPassword }));
    }

    [Fact]
    public async Task NewRequest_InvalidatesThePreviousLink()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        var first = await RequestTokenAsync(db);
        var second = await RequestTokenAsync(db);

        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<DomainValidationException>(() =>
                scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = first, NewPassword = NewPassword }));

        using (var scope = db.NewScope())
            await scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = second, NewPassword = NewPassword });
    }

    [Fact]
    public async Task Reset_WithGarbageToken_OrWeakPassword_IsRejected()
    {
        using var db = new TestDb();
        TestData.Seed(db);
        var token = await RequestTokenAsync(db);

        using var scope = db.NewScope();
        var bad = await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = "uydurma", NewPassword = NewPassword }));
        Assert.True(bad.Errors.ContainsKey("Token"));

        var weak = await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = token, NewPassword = "kisa" }));
        Assert.True(weak.Errors.ContainsKey("NewPassword"));

        // zayıf parola denemesi jetonu harcamaz
        using var retry = db.NewScope();
        await retry.PasswordReset.ResetAsync(new ResetPasswordRequest { Token = token, NewPassword = NewPassword });
    }
}
