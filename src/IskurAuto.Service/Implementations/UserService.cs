using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;
using IskurAuto.Service.Validation;

namespace IskurAuto.Service.Implementations;

/// <summary>Personel hesaplarının yönetimi (belge 2.1: "Kullanıcı, bölüm, danışman ... kayıtlarını yönetme").</summary>
public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public UserService(
        IUserRepository users, IDepartmentRepository departments, IUnitOfWork uow,
        IPasswordHasher hasher, IAuditService audit, ICurrentUser current)
    {
        _users = users;
        _departments = departments;
        _uow = uow;
        _hasher = hasher;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<UserDto>> SearchAsync(UserSearchQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var criteria = new UserSearchCriteria
        {
            Role = query.Role,
            DepartmentId = query.DepartmentId,
            IsActive = query.IsActive,
            RegistryNumber = string.IsNullOrWhiteSpace(query.RegistryNumber) ? null : InputNormalizer.RegistryNumber(query.RegistryNumber)
        };

        // Bölüm başkanı (örn. danışman seçmek için) yalnızca kendi bölümündeki personeli listeleyebilir.
        if (_current.Role == UserRole.DepartmentHead)
            criteria.DepartmentId = _current.DepartmentId
                ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış.");

        var users = await _users.SearchAsync(criteria, ct);
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> CreateStaffAsync(CreateStaffUserRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var email = InputNormalizer.Email(request.Email);
        var fullName = InputNormalizer.Name(request.FullName);
        var registry = InputNormalizer.RegistryNumber(request.RegistryNumber);
        var needsDepartment = request.Role is UserRole.DepartmentHead or UserRole.Advisor;

        var errors = new ValidationErrors();
        if (!Enum.IsDefined(request.Role) || request.Role == UserRole.Student)
            errors.Add(nameof(request.Role), "Rol fakülte sekreterliği, birim yetkilisi veya sorumlu personel olmalıdır. Öğrenci hesapları öğrenci ekranından açılır.");
        if (registry.Length < 3)
            errors.Add(nameof(request.RegistryNumber), "Sicil numarası en az 3 karakter olmalıdır.");
        if (fullName.Length < 2)
            errors.Add(nameof(request.FullName), "Ad soyad en az 2 karakter olmalıdır.");
        errors.Add(nameof(request.Password), PasswordPolicy.Validate(request.Password));

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            phone = InputNormalizer.Phone(request.PhoneNumber);
            if (!InputNormalizer.IsValidMobile(phone))
                errors.Add(nameof(request.PhoneNumber), "Geçerli bir cep telefonu numarası giriniz.");
        }

        Department? department = null;
        if (needsDepartment)
        {
            department = request.DepartmentId is int id ? await _departments.GetByIdAsync(id) : null;
            if (department is null || !department.IsActive)
                errors.Add(nameof(request.DepartmentId), "Bu rol için geçerli, aktif bir bölüm seçilmelidir.");
        }
        errors.ThrowIfAny();

        if (await _users.EmailExistsAsync(email, ct))
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap zaten var.");
        if (await _users.RegistryNumberExistsAsync(registry, ct: ct))
            throw new ConflictException("Bu sicil numarasıyla kayıtlı bir personel zaten var.");

        // Bir bölümde aynı anda tek aktif bölüm başkanı olur (başkan değişirken eskisi önce pasifleştirilir).
        if (request.Role == UserRole.DepartmentHead)
            await EnsureNoActiveHeadAsync(department!.Id, ct);

        var user = new User
        {
            FullName = fullName,
            Email = email,
            RegistryNumber = registry,
            PhoneNumber = phone,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role,
            DepartmentId = department?.Id,      // FacultyAdmin için null
            Department = department,
            IsActive = true
        };
        await _users.AddAsync(user);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.StaffUserCreated, nameof(User), user.Id,
            $"Personel hesabı açıldı. Rol: {TurkishText.Role(user.Role)}", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return user.ToDto();
    }

    public async Task<UserDto> SetActiveAsync(int userId, bool isActive, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var user = await _users.GetWithDepartmentAsync(userId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        if (user.Role == UserRole.Student)
            throw new DomainValidationException("Öğrenci hesapları, program katılımı (aktif/pasif) üzerinden yönetilir.");

        if (user.Id == _current.UserId && !isActive)
            throw new ConflictException("Kendi hesabınızı pasifleştiremezsiniz.");

        if (user.IsActive == isActive)
            return user.ToDto();

        if (isActive && user.Role == UserRole.DepartmentHead && user.DepartmentId is int deptId)
            await EnsureNoActiveHeadAsync(deptId, ct);

        user.IsActive = isActive;
        await _audit.RecordAsync(isActive ? AuditActions.UserActivated : AuditActions.UserDeactivated,
            nameof(User), user.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task<UserDto> UpdateStaffAsync(int userId, UpdateStaffUserRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var user = await _users.GetWithDepartmentAsync(userId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");
        if (user.Role == UserRole.Student)
            throw new DomainValidationException("Öğrenci bilgileri öğrenci ekranından düzenlenir.");

        var email = InputNormalizer.Email(request.Email);
        var fullName = InputNormalizer.Name(request.FullName);
        var registry = InputNormalizer.RegistryNumber(request.RegistryNumber);
        var needsDepartment = request.Role is UserRole.DepartmentHead or UserRole.Advisor;

        var errors = new ValidationErrors();
        if (!Enum.IsDefined(request.Role) || request.Role == UserRole.Student)
            errors.Add(nameof(request.Role), "Rol fakülte sekreterliği, birim yetkilisi veya sorumlu personel olmalıdır.");
        if (fullName.Length < 2)
            errors.Add(nameof(request.FullName), "Ad soyad en az 2 karakter olmalıdır.");
        if (registry.Length < 3)
            errors.Add(nameof(request.RegistryNumber), "Sicil numarası en az 3 karakter olmalıdır.");
        if (!string.IsNullOrEmpty(request.NewPassword))
            errors.Add(nameof(request.NewPassword), PasswordPolicy.Validate(request.NewPassword));
        if (user.Id == _current.UserId && request.Role != UserRole.FacultyAdmin)
            errors.Add(nameof(request.Role), "Kendi rolünüzü değiştiremezsiniz.");

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            phone = InputNormalizer.Phone(request.PhoneNumber);
            if (!InputNormalizer.IsValidMobile(phone))
                errors.Add(nameof(request.PhoneNumber), "Geçerli bir cep telefonu numarası giriniz.");
        }

        Department? department = null;
        if (needsDepartment)
        {
            department = request.DepartmentId is int id ? await _departments.GetByIdAsync(id) : null;
            if (department is null || !department.IsActive)
                errors.Add(nameof(request.DepartmentId), "Bu rol için geçerli, aktif bir bölüm seçilmelidir.");
        }
        errors.ThrowIfAny();

        if (await _users.EmailExistsExceptAsync(email, user.Id, ct))
            throw new ConflictException("Bu e-posta adresiyle kayıtlı başka bir hesap var.");
        if (await _users.RegistryNumberExistsAsync(registry, user.Id, ct))
            throw new ConflictException("Bu sicil numarasıyla kayıtlı başka bir personel var.");

        var becomesHead = request.Role == UserRole.DepartmentHead
            && user.IsActive
            && (user.Role != UserRole.DepartmentHead || user.DepartmentId != department!.Id);
        if (becomesHead)
            await EnsureNoActiveHeadAsync(department!.Id, ct);

        user.FullName = fullName;
        user.Email = email;
        user.RegistryNumber = registry;
        user.PhoneNumber = phone;
        user.Role = request.Role;
        user.DepartmentId = department?.Id;
        user.Department = department;
        if (!string.IsNullOrEmpty(request.NewPassword))
            user.PasswordHash = _hasher.Hash(request.NewPassword);

        await _audit.RecordAsync(AuditActions.StaffUserUpdated, nameof(User), user.Id,
            $"Personel bilgileri güncellendi. Rol: {TurkishText.Role(user.Role)}", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return user.ToDto();
    }

    private async Task EnsureNoActiveHeadAsync(int departmentId, CancellationToken ct)
    {
        var heads = await _users.SearchAsync(new UserSearchCriteria
        {
            Role = UserRole.DepartmentHead,
            DepartmentId = departmentId,
            IsActive = true
        }, ct);

        if (heads.Count > 0)
            throw new ConflictException("Bu bölümde zaten aktif bir birim yetkilisi var. Önce mevcut birim yetkilisini pasifleştirin.");
    }
}
