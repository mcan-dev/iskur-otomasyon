using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class DepartmentRepository : GenericRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(IskurAutoDbContext context) : base(context) { }

    public Task<bool> CodeExistsAsync(string code, int? exceptId = null, CancellationToken ct = default)
        => Context.Departments.AnyAsync(d => d.Code == code && (exceptId == null || d.Id != exceptId), ct);
}

public class WorkUnitRepository : GenericRepository<WorkUnit>, IWorkUnitRepository
{
    public WorkUnitRepository(IskurAutoDbContext context) : base(context) { }

    public Task<bool> NameExistsAsync(string name, int? exceptId = null, CancellationToken ct = default)
        => Context.WorkUnits.AnyAsync(w => w.Name == name && (exceptId == null || w.Id != exceptId), ct);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IskurAutoDbContext _context;

    public AuditLogRepository(IskurAutoDbContext context) => _context = context;

    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
        => await _context.AuditLogs.AddAsync(entry, ct);

    public async Task<PagedResult<AuditLog>> SearchAsync(
        AuditLogSearchCriteria criteria, PageRequest page, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (criteria.UserId.HasValue)               query = query.Where(a => a.UserId == criteria.UserId.Value);
        if (!string.IsNullOrWhiteSpace(criteria.Action))     query = query.Where(a => a.Action == criteria.Action);
        if (!string.IsNullOrWhiteSpace(criteria.EntityName)) query = query.Where(a => a.EntityName == criteria.EntityName);
        if (criteria.FromUtc.HasValue)              query = query.Where(a => a.OccurredAt >= criteria.FromUtc.Value);
        if (criteria.ToUtc.HasValue)                query = query.Where(a => a.OccurredAt <= criteria.ToUtc.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLog>
        {
            Items = items,
            TotalCount = total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }
}
