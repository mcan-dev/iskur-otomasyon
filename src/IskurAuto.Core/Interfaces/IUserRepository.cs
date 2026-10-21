using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    /// <summary>E-postaya göre kullanıcıyı (bölümüyle birlikte, izlenen halde) getirir.</summary>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);

    /// <summary>Id'ye göre kullanıcıyı bölümüyle birlikte getirir (izlenen halde).</summary>
    Task<User?> GetWithDepartmentAsync(int id, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);

    Task<IReadOnlyList<User>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct = default);
}
