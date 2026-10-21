using System.Linq.Expressions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

/// <summary>
/// <see cref="IRepository{T}"/> arayüzünün EF Core ile genel uygulaması.
/// SaveChanges çağırmaz; kaydetme <see cref="IUnitOfWork"/> ile servis katmanında yapılır.
/// Özel sorgular gereken tablolar bu sınıftan türeyen repository'lerde genişletilir.
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly IskurAutoDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(IskurAutoDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<T>();
    }

    // Okuma: tekil getirme izlenir (tracked) çünkü genelde getirip değiştirmek içindir;
    // toplu listeleme izlenmez (AsNoTracking) çünkü salt okunurdur ve daha hızlıdır.
    public async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await DbSet.AsNoTracking().ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await DbSet.AsNoTracking().Where(predicate).ToListAsync();

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => await DbSet.FirstOrDefaultAsync(predicate);

    public async Task AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await DbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        DbSet.RemoveRange(entities);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => await DbSet.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate) => await DbSet.CountAsync(predicate);
}
