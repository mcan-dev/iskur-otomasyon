namespace IskurAuto.Core.Entities;

/// <summary>
/// Fakültenin bölümü (örn. Bilgisayar Mühendisliği).
/// Bölüm Başkanı ve Danışman yetki kapsamını belirleyen ana sınırdır.
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Kısa, benzersiz kod (örn. "BM").</summary>
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
