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

/// <summary>Giriş, öğrenci kaydı ve parola değiştirme.</summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AuthService(
        IUserRepository users, IStudentRepository students, IDepartmentRepository departments,
        IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens,
        IClock clock, IAuditService audit, ICurrentUser current)
    {
        _users = users;
        _students = students;
        _departments = departments;
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = InputNormalizer.Email(request.Email);
        var user = await _users.GetByEmailAsync(email, ct);

        // Kullanıcı yoksa bile sahte özet üzerinde doğrulama yapılır: yanıt süresi aynı kalır,
        // saldırgan "bu e-posta kayıtlı mı?" bilgisini süreden çıkaramaz.
        var passwordOk = _hasher.Verify(user?.PasswordHash ?? _hasher.DummyHash, request.Password);

        if (user is null || !passwordOk || !user.IsActive)
        {
            await _audit.RecordAsync(AuditActions.LoginFailed, nameof(User), user?.Id,
                "Başarısız giriş denemesi.", actorUserId: user?.Id, actorEmail: email, ct: ct);
            await _uow.SaveChangesAsync(ct);

            // Hangi koşulun başarısız olduğu söylenmez: hep aynı mesaj.
            throw new AuthenticationFailedException();
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

    public async Task<RegistrationResultDto> RegisterStudentAsync(StudentRegistrationRequest request, CancellationToken ct = default)
    {
        // 1) Normalizasyon: girdiyi tek biçime getir (boşluk, büyük/küçük harf, telefon biçimi).
        var email = InputNormalizer.Email(request.Email);
        var fullName = InputNormalizer.Name(request.FullName);
        var phone = InputNormalizer.Phone(request.PhoneNumber);
        var iban = IbanValidator.Normalize(request.Iban);
        var nationalId = (request.NationalId ?? string.Empty).Trim();
        var studentNumber = (request.StudentNumber ?? string.Empty).Trim();

        // 2) Anlamsal doğrulama: tüm hatalar toplanır, kullanıcı formda hepsini birden görür.
        var errors = new ValidationErrors();
        if (fullName.Length < 2) errors.Add(nameof(request.FullName), "Ad soyad en az 2 karakter olmalıdır.");
        errors.Add(nameof(request.Password), PasswordPolicy.Validate(request.Password));
        if (!NationalIdValidator.IsValid(nationalId))
            errors.Add(nameof(request.NationalId), "T.C. kimlik numarası geçersiz.");
        if (!IbanValidator.IsValid(iban))
            errors.Add(nameof(request.Iban), "IBAN geçersiz. TR ile başlayan, 26 haneli geçerli bir IBAN giriniz.");
        if (!InputNormalizer.IsValidMobile(phone))
            errors.Add(nameof(request.PhoneNumber), "Geçerli bir cep telefonu numarası giriniz (örn. 0532 123 45 67).");
        if (request.RegistrationYear > _clock.Today.Year)
            errors.Add(nameof(request.RegistrationYear), "Kayıt yılı gelecekte olamaz.");

        var department = await _departments.GetByIdAsync(request.DepartmentId);
        if (department is null || !department.IsActive)
            errors.Add(nameof(request.DepartmentId), "Seçilen bölüm bulunamadı.");

        errors.ThrowIfAny();

        // 3) Mükerrer kayıt kontrolü (veritabanındaki benzersiz indeksler ikinci güvence).
        if (await _users.EmailExistsAsync(email, ct))
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap zaten var.");
        if (await _students.NationalIdExistsAsync(nationalId, ct))
            throw new ConflictException("Bu T.C. kimlik numarasıyla kayıtlı bir öğrenci zaten var.");
        if (await _students.StudentNumberExistsAsync(studentNumber, ct))
            throw new ConflictException("Bu öğrenci numarasıyla kayıtlı bir öğrenci zaten var.");

        // 4) Üç kayıt (kullanıcı, profil, ilk katılım dönemi) tek işlemde yazılır.
        var profile = new StudentProfile
        {
            User = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                PasswordHash = _hasher.Hash(request.Password),
                Role = UserRole.Student,
                IsActive = true
            },
            NationalId = nationalId,
            Iban = iban,
            StudentNumber = studentNumber,
            DepartmentId = department!.Id,
            RegistrationYear = request.RegistrationYear
        };
        profile.Participations.Add(new ProgramParticipation { StartDate = _clock.Today });

        await _students.AddAsync(profile);

        // Denetim kaydı yeni Id'yi bilmek ister; bu yüzden önce kaydedilir, sonra denetim yazılır.
        // İkisi aynı işlem (transaction) içinde olduğundan biri başarısızsa ikisi de geri alınır.
        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.StudentRegistered, nameof(StudentProfile), profile.Id,
            "Öğrenci kendi kaydını oluşturdu.", actorUserId: profile.UserId, actorEmail: email, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new RegistrationResultDto { UserId = profile.UserId, StudentId = profile.Id, Email = email };
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated)
            throw new AuthenticationFailedException("Bu işlem için oturum açmanız gerekiyor.");

        var user = await _users.GetByIdAsync(_current.UserId)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

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
