using System.Linq.Expressions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

/// <summary>
/// <see cref="IRepository{T}"/> arayüzünün Entity Framework Core ile
/// çalışan genel (generic) uygulaması.
/// Tüm entity tipleri için temel CRUD ve sorgulama operasyonlarını sağlar.
/// SaveChangesAsync çağrısı bu sınıfın sorumluluğunda değildir;
/// Unit of Work deseni ile dışarıdan yönetilir.
/// </summary>
/// <typeparam name="T">Yönetilecek EF Core entity türü.</typeparam>
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly IskurAutoDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(IskurAutoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    // ─── Okuma Operasyonları ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.AsNoTracking().ToListAsync();

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);

    // ─── Yazma Operasyonları ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity);
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _dbSet.AddRangeAsync(entities);
    }

    /// <inheritdoc/>
    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Update(entity);
    }

    /// <inheritdoc/>
    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Remove(entity);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _dbSet.RemoveRange(entities);
    }

    // ─── Yardımcı Operasyonlar ────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

    /// <inheritdoc/>
    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.CountAsync(predicate);
}
