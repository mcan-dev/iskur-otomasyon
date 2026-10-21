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

/// <summary>Bölüm tanımları. Listeleme herkese açıktır (kayıt formundaki bölüm seçimi için).</summary>
public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public DepartmentService(IDepartmentRepository departments, IUnitOfWork uow, IAuditService audit, ICurrentUser current)
    {
        _departments = departments;
        _uow = uow;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<DepartmentDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        // Pasif bölümleri yalnızca fakülte yönetimi görebilir; diğerleri için istek sessizce yok sayılır.
        var showInactive = includeInactive && _current.Role == UserRole.FacultyAdmin;

        var all = await _departments.GetAllAsync();
        return all.Where(d => showInactive || d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => d.ToDto())
            .ToList();
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentUpsertRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var (name, code) = Validate(request);

        if (await _departments.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"'{code}' kodlu bir bölüm zaten var.");

        var department = new Department { Name = name, Code = code, IsActive = request.IsActive };
        await _departments.AddAsync(department);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.DepartmentCreated, nameof(Department), department.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return department.ToDto();
    }

    public async Task<DepartmentDto> UpdateAsync(int id, DepartmentUpsertRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var (name, code) = Validate(request);

        var department = await _departments.GetByIdAsync(id)
            ?? throw new NotFoundException("Bölüm bulunamadı.");

        if (await _departments.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"'{code}' kodlu başka bir bölüm zaten var.");

        department.Name = name;
        department.Code = code;
        department.IsActive = request.IsActive;

        await _audit.RecordAsync(AuditActions.DepartmentUpdated, nameof(Department), department.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return department.ToDto();
    }

    private static (string Name, string Code) Validate(DepartmentUpsertRequest request)
    {
        var name = InputNormalizer.Name(request.Name);
        var code = request.Code.Trim().ToUpperInvariant();     // "bm" ile "BM" aynı kod sayılır

        var errors = new ValidationErrors();
        if (name.Length < 2) errors.Add(nameof(request.Name), "Bölüm adı en az 2 karakter olmalıdır.");
        if (code.Length < 1) errors.Add(nameof(request.Code), "Bölüm kodu boş olamaz.");
        errors.ThrowIfAny();

        return (name, code);
    }
}

/// <summary>Çalışma birimleri (Bilgisayar Laboratuvarı, Dekanlık Ofisi...). Yönetimi fakülte yönetiminindir.</summary>
public class WorkUnitService : IWorkUnitService
{
    private readonly IWorkUnitRepository _units;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public WorkUnitService(IWorkUnitRepository units, IUnitOfWork uow, IAuditService audit, ICurrentUser current)
    {
        _units = units;
        _uow = uow;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<WorkUnitDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var all = await _units.GetAllAsync();
        return all.Where(u => includeInactive || u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => u.ToDto())
            .ToList();
    }

    public async Task<WorkUnitDto> CreateAsync(WorkUnitUpsertRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var name = ValidateName(request);

        if (await _units.NameExistsAsync(name, null, ct))
            throw new ConflictException($"'{name}' adlı bir birim zaten var.");

        var unit = new WorkUnit
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };
        await _units.AddAsync(unit);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.WorkUnitCreated, nameof(WorkUnit), unit.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return unit.ToDto();
    }

    public async Task<WorkUnitDto> UpdateAsync(int id, WorkUnitUpsertRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var name = ValidateName(request);

        var unit = await _units.GetByIdAsync(id)
            ?? throw new NotFoundException("Birim bulunamadı.");

        if (await _units.NameExistsAsync(name, id, ct))
            throw new ConflictException($"'{name}' adlı başka bir birim zaten var.");

        unit.Name = name;
        unit.Description = request.Description?.Trim();
        unit.IsActive = request.IsActive;

        await _audit.RecordAsync(AuditActions.WorkUnitUpdated, nameof(WorkUnit), unit.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return unit.ToDto();
    }

    private static string ValidateName(WorkUnitUpsertRequest request)
    {
        var name = InputNormalizer.Name(request.Name);
        if (name.Length < 2)
            throw new DomainValidationException(nameof(request.Name), "Birim adı en az 2 karakter olmalıdır.");
        return name;
    }
}
