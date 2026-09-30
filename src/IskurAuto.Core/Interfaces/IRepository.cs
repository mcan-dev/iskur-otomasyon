using System.Linq.Expressions;

namespace IskurAuto.Core.Interfaces;

/// <summary>
/// Tüm entity'ler için standart CRUD ve sorgulama operasyonlarını tanımlayan
/// jenerik repository sözleşmesi (contract).
/// </summary>
/// <typeparam name="T">Repository'nin yöneteceği entity türü.</typeparam>
public interface IRepository<T> where T : class
{
    // --- Okuma Operasyonları ---

    /// <summary>Birincil anahtar (PK) ile entity getirir. Bulunamazsa null döner.</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Tablo içindeki tüm kayıtları getirir.</summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Verilen koşula (predicate) uyan kayıtları getirir.
    /// Örn: repo.FindAsync(u => u.FacultyId == facultyId)
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Koşula uyan ilk kaydı getirir. Bulunamazsa null döner.</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    // --- Yazma Operasyonları ---

    /// <summary>Yeni bir entity ekler (SaveChanges çağrılmaz).</summary>
    Task AddAsync(T entity);

    /// <summary>Birden fazla entity'yi tek seferde ekler (SaveChanges çağrılmaz).</summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>Var olan bir entity'yi günceller (SaveChanges çağrılmaz).</summary>
    void Update(T entity);

    /// <summary>Entity'yi siler (SaveChanges çağrılmaz).</summary>
    void Remove(T entity);

    /// <summary>Birden fazla entity'yi siler (SaveChanges çağrılmaz).</summary>
    void RemoveRange(IEnumerable<T> entities);

    // --- Yardımcı Operasyonlar ---

    /// <summary>Koşula uyan en az bir kayıt olup olmadığını kontrol eder.</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Koşula uyan kayıt sayısını döner.</summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}
