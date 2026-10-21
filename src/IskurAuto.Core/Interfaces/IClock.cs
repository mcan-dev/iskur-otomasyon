namespace IskurAuto.Core.Interfaces;

/// <summary>
/// Zaman kaynağı soyutlaması. Servisler DateTime.UtcNow'u doğrudan çağırmaz;
/// böylece testlerde zaman sabitlenebilir (özellikle 30 sn'lik karekod ve izin tarihi kuralları için şart).
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Türkiye saatine göre bugünün tarihi (UTC+3, yıl boyu sabit).</summary>
    DateOnly Today { get; }
}
