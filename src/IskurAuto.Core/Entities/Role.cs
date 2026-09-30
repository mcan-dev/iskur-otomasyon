namespace IskurAuto.Core.Entities;

/// <summary>
/// Sistem rollerini tanımlar.
/// Maker (FacultySecretary) ve Checker (SksAdmin) yetkilendirme modeli.
/// </summary>
public class Role
{
    public int Id { get; set; }

    /// <summary>Rol adı. Örn: "FacultySecretary", "SksAdmin"</summary>
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Navigation Properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
