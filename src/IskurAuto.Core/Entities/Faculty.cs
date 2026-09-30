namespace IskurAuto.Core.Entities;

/// <summary>
/// Multi-Tenant yapısının temelini oluşturan fakülte tanım entity'si.
/// Her fakülte bir kiracı (tenant) olarak işlev görür.
/// </summary>
public class Faculty
{
    public int Id { get; set; }

    /// <summary>Fakültenin tam resmi adı. Örn: "Mühendislik Fakültesi"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Fakültenin kısa kodu. Örn: "MF"</summary>
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<StudentWorker> StudentWorkers { get; set; } = new List<StudentWorker>();
}
