using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;
using IskurAuto.Service.Validation;

namespace IskurAuto.Service.Implementations;

/// <summary>Giriş (öğrenci e-postası + OBİS şifresi, personel sicil no + şifre) ve personel şifre değiştirme.</summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AuthService(
        IUserRepository users, IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens,
        IClock clock, IAuditService audit, ICurrentUser current)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.Role))
            throw new DomainValidationException(nameof(request.Role), "Giriş türü seçilmelidir.");

        // Öğrenci öğrenci e-postasıyla, personel sicil numarasıyla girer.
        var isStudentLogin = request.Role == UserRole.Student;
        var username = isStudentLogin
            ? InputNormalizer.Email(request.Username)
            : InputNormalizer.RegistryNumber(request.Username);

        var user = isStudentLogin
            ? await _users.GetByEmailAsync(username, ct)
            : await _users.GetByRegistryNumberAsync(username, ct);

        // Kullanıcı yoksa bile sahte özet üzerinde doğrulama yapılır: yanıt süresi aynı kalır,
        // saldırgan "bu hesap kayıtlı mı?" bilgisini süreden çıkaramaz.
        var passwordOk = _hasher.Verify(user?.PasswordHash ?? _hasher.DummyHash, request.Password);

        // Parolası doğru olan ama hesabı aktif olmayan kişiye nedeni söylemek güvenlidir: parolayı zaten biliyor.
        if (user is { IsActive: false } && passwordOk)
        {
            await _audit.RecordAsync(AuditActions.LoginFailed, nameof(User), user.Id,
                "Pasif hesapla giriş denemesi.", actorUserId: user.Id, actorEmail: user.Email, ct: ct);
            await _uow.SaveChangesAsync(ct);
            throw new ForbiddenException("Hesabınız aktif değil. Lütfen fakülte sekreterliği ile iletişime geçin.");
        }

        if (user is null || !passwordOk || !user.IsActive)
        {
            await _audit.RecordAsync(AuditActions.LoginFailed, nameof(User), user?.Id,
                "Başarısız giriş denemesi.", actorUserId: user?.Id, actorEmail: user?.Email, ct: ct);
            await _uow.SaveChangesAsync(ct);

            // Hangi koşulun başarısız olduğu söylenmez: hep aynı mesaj.
            throw new AuthenticationFailedException(isStudentLogin
                ? "Öğrenci e-postası veya OBİS şifresi hatalı."
                : "Sicil numarası veya şifre hatalı.");
        }

        // Parola doğru ama hesap bu giriş sayfasının rolünde değil: jeton verilmez.
        if (user.Role != request.Role)
        {
            await _audit.RecordAsync(AuditActions.LoginFailed, nameof(User), user.Id,
                "Hesap, giriş sayfasının rolüyle eşleşmiyor.", actorUserId: user.Id, actorEmail: user.Email, ct: ct);
            await _uow.SaveChangesAsync(ct);
            throw new ForbiddenException("Bu hesap bu giriş türü için yetkili değil. Lütfen kendi rolünüzün giriş sayfasını kullanın.");
        }

        user.LastLoginAt = _clock.UtcNow;
        await _audit.RecordAsync(AuditActions.LoginSucceeded, nameof(User), user.Id,
            actorUserId: user.Id, actorEmail: user.Email, ct: ct);
        await _uow.SaveChangesAsync(ct);

        var token = _tokens.CreateToken(user);
        return new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = user.ToDto()
        };
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated)
            throw new AuthenticationFailedException("Bu işlem için oturum açmanız gerekiyor.");

        var user = await _users.GetByIdAsync(_current.UserId)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        // Öğrencinin şifresi OBİS şifresidir; OBİS üzerinden değiştirilir, bu sistemde değiştirilemez.
        if (user.Role == UserRole.Student)
            throw new ForbiddenException("Öğrenci şifresi OBİS şifresidir; şifrenizi OBİS üzerinden değiştirebilirsiniz.");

        if (!_hasher.Verify(user.PasswordHash, request.CurrentPassword))
            throw new DomainValidationException(nameof(request.CurrentPassword), "Mevcut parola hatalı.");

        var policyError = PasswordPolicy.Validate(request.NewPassword);
        if (policyError is not null)
            throw new DomainValidationException(nameof(request.NewPassword), policyError);

        if (request.NewPassword == request.CurrentPassword)
            throw new DomainValidationException(nameof(request.NewPassword), "Yeni parola eskisiyle aynı olamaz.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        await _audit.RecordAsync(AuditActions.PasswordChanged, nameof(User), user.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated)
            throw new AuthenticationFailedException("Bu işlem için oturum açmanız gerekiyor.");

        var user = await _users.GetWithDepartmentAsync(_current.UserId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");
        return user.ToDto();
    }
}
