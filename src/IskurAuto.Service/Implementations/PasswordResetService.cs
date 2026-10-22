using System.Security.Cryptography;
using System.Text;
using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Validation;

namespace IskurAuto.Service.Implementations;

/// <summary>Parolamı unuttum akışı: tek kullanımlık, süreli bağlantı.</summary>
public class PasswordResetService : IPasswordResetService
{
    private const string InvalidLinkMessage = "Bağlantı geçersiz veya süresi dolmuş. Yeni bir bağlantı isteyin.";

    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _email;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly PasswordResetSettings _settings;

    public PasswordResetService(
        IUserRepository users, IPasswordResetTokenRepository tokens, IUnitOfWork uow,
        IPasswordHasher hasher, IEmailSender email, IClock clock, IAuditService audit,
        PasswordResetSettings settings)
    {
        _users = users;
        _tokens = tokens;
        _uow = uow;
        _hasher = hasher;
        _email = email;
        _clock = clock;
        _audit = audit;
        _settings = settings;
    }

    public async Task RequestAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var address = InputNormalizer.Email(request.Email);
        var user = await _users.GetByEmailAsync(address, ct);

        // Kayıtlı olmayan veya pasif hesapta sessizce çıkılır; dışarıdan bakan fark göremez.
        if (user is null || !user.IsActive) return;

        var now = _clock.UtcNow;
        foreach (var open in await _tokens.GetUnusedForUserAsync(user.Id, ct))
            open.UsedAt = now;

        var token = NewToken();
        await _tokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = now.AddMinutes(_settings.TokenMinutes)
        });
        await _audit.RecordAsync(AuditActions.PasswordResetRequested, nameof(User), user.Id,
            actorUserId: user.Id, actorEmail: user.Email, ct: ct);
        await _uow.SaveChangesAsync(ct);

        var link = $"{_settings.FrontendBaseUrl.TrimEnd('/')}/sifre-sifirla?token={token}";
        var body =
            $"Merhaba {user.FullName},\n\n" +
            "İŞKUR Otomasyonu hesabınız için parola sıfırlama isteği aldık. " +
            $"Yeni parolanızı {_settings.TokenMinutes} dakika içinde şu bağlantıdan belirleyebilirsiniz:\n\n{link}\n\n" +
            "Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz; parolanız değişmez.";
        await _email.SendAsync(user.Email, "Parola sıfırlama", body, ct);
    }

    public async Task ResetAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var policyError = PasswordPolicy.Validate(request.NewPassword);
        if (policyError is not null)
            throw new DomainValidationException(nameof(request.NewPassword), policyError);

        var record = await _tokens.GetByHashAsync(Hash((request.Token ?? string.Empty).Trim()), ct);
        var now = _clock.UtcNow;
        if (record is null || record.UsedAt is not null || record.ExpiresAt <= now)
            throw new DomainValidationException(nameof(request.Token), InvalidLinkMessage);

        var user = await _users.GetByIdAsync(record.UserId);
        if (user is null || !user.IsActive)
            throw new DomainValidationException(nameof(request.Token), InvalidLinkMessage);

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        record.UsedAt = now;
        foreach (var other in await _tokens.GetUnusedForUserAsync(user.Id, ct))
            other.UsedAt = now;

        await _audit.RecordAsync(AuditActions.PasswordResetCompleted, nameof(User), user.Id,
            actorUserId: user.Id, actorEmail: user.Email, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    private static string NewToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
