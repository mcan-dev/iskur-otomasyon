namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrencinin çalıştığı birim (örn. "Bilgisayar Laboratuvarı", "Dekanlık Ofisi").
/// Bölümden ayrı bir kavramdır: öğrenci kendi bölümünden farklı bir birimde çalışabilir.
/// </summary>
public class WorkUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
