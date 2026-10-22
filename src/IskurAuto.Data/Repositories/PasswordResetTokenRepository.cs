using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(IskurAutoDbContext context) : base(context) { }

    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<PasswordResetToken>> GetUnusedForUserAsync(int userId, CancellationToken ct = default)
        => await DbSet.Where(t => t.UserId == userId && t.UsedAt == null).ToListAsync(ct);
}
