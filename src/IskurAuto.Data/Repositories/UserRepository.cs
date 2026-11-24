using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IskurAutoDbContext context) : base(context) { }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default)
        => Context.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

    public Task<User?> GetByRegistryNumberAsync(string registryNumber, CancellationToken ct = default)
        => Context.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.RegistryNumber == registryNumber, ct);

    public Task<bool> RegistryNumberExistsAsync(string registryNumber, int? exceptUserId = null, CancellationToken ct = default)
        => Context.Users.AnyAsync(u => u.RegistryNumber == registryNumber && (exceptUserId == null || u.Id != exceptUserId), ct);

    public Task<bool> EmailExistsExceptAsync(string normalizedEmail, int exceptUserId, CancellationToken ct = default)
        => Context.Users.AnyAsync(u => u.Email == normalizedEmail && u.Id != exceptUserId, ct);

    public Task<User?> GetWithDepartmentAsync(int id, CancellationToken ct = default)
        => Context.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default)
        => Context.Users.AnyAsync(u => u.Email == normalizedEmail, ct);

    public async Task<IReadOnlyList<User>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = Context.Users.AsNoTracking().Include(u => u.Department).AsQueryable();

        if (criteria.Role.HasValue)         query = query.Where(u => u.Role == criteria.Role.Value);
        if (criteria.DepartmentId.HasValue) query = query.Where(u => u.DepartmentId == criteria.DepartmentId.Value);
        if (criteria.IsActive.HasValue)     query = query.Where(u => u.IsActive == criteria.IsActive.Value);
        if (criteria.RegistryNumber is { Length: > 0 } registry) query = query.Where(u => u.RegistryNumber == registry);

        return await query.OrderBy(u => u.FullName).ThenBy(u => u.Id).ToListAsync(ct);
    }
}
