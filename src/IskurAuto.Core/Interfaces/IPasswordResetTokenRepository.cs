using IskurAuto.Core.Entities;

namespace IskurAuto.Core.Interfaces;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    /// <summary>Özete göre jetonu getirir (izlenen halde).</summary>
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Kullanıcının henüz kullanılmamış jetonlarını getirir (izlenen halde).</summary>
    Task<IReadOnlyList<PasswordResetToken>> GetUnusedForUserAsync(int userId, CancellationToken ct = default);
}
