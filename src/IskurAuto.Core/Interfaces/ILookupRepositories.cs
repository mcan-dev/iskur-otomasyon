using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface IDepartmentRepository : IRepository<Department>
{
    /// <param name="exceptId">Güncellemede kaydın kendisini hariç tutmak için.</param>
    Task<bool> CodeExistsAsync(string code, int? exceptId = null, CancellationToken ct = default);
}

public interface IWorkUnitRepository : IRepository<WorkUnit>
{
    Task<bool> NameExistsAsync(string name, int? exceptId = null, CancellationToken ct = default);
}

/// <summary>
/// Denetim kaydı deposu: bilerek yalnızca ekleme ve sorgulama sunar (değiştirme/silme yok).
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken ct = default);

    Task<PagedResult<AuditLog>> SearchAsync(
        AuditLogSearchCriteria criteria, PageRequest page, CancellationToken ct = default);
}
