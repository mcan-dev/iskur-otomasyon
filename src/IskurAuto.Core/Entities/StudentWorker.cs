namespace IskurAuto.Core.Entities;

/// <summary>
/// Kısmi zamanlı (part-time) çalışan öğrencilerin bilgilerini içerir.
/// FacultyId üzerinden Multi-Tenant izolasyonu sağlanır.
/// </summary>
public class StudentWorker
{
    public int Id { get; set; }

    /// <summary>TC Kimlik Numarası (11 hane).</summary>
    public string NationalId { get; set; } = string.Empty;

    /// <summary>
    /// Üniversiteye özgü öğrenci numarası.
    /// İŞKUR süreci TC ile, üniversite süreci bu numara ile yürütülür.
    /// </summary>
    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Öğrencinin İŞKUR sistemindeki kayıt numarası (varsa).</summary>
    public string? IskurRegistrationNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int FacultyId { get; set; }

    // Navigation Properties
    public Faculty Faculty { get; set; } = null!;
    public ICollection<MonthlyTimesheet> MonthlyTimesheets { get; set; } = new List<MonthlyTimesheet>();
}
