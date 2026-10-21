namespace IskurAuto.Core.Entities;

/// <summary>
/// Değiştirilemez (append-only) denetim kaydı: "kim, ne zaman, neye ne yaptı".
/// BaseEntity'den türemez çünkü UpdatedAt gibi alanlara ihtiyaç yoktur ve
/// repository'si bilerek Update/Remove sunmaz.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>İşlemi yapan kullanıcı; giriş denemesi gibi anonim işlemlerde null olabilir.</summary>
    public int? UserId { get; set; }

    /// <summary>Kullanıcı sonradan silinse/değişse bile iz kalsın diye e-posta kopyalanır.</summary>
    public string? UserEmail { get; set; }

    /// <summary>Bkz. <see cref="Constants.AuditActions"/>.</summary>
    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    /// <summary>İnsan tarafından okunabilir açıklama. Kişisel veri (TC, IBAN) içermemelidir.</summary>
    public string? Details { get; set; }

    public string? IpAddress { get; set; }
}
