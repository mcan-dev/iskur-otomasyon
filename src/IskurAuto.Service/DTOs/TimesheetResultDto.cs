using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

/// <summary>
/// Puantaj verilerini API katmanına döndürmek için kullanılan okuma DTO'su.
/// Entity'nin tüm alanlarını içerir; navigation property'ler flatten edilmiştir.
/// </summary>
public class TimesheetResultDto
{
    public int Id { get; set; }
    public int StudentWorkerId { get; set; }

    /// <summary>Öğrencinin tam adı (FirstName + LastName).</summary>
    public string StudentFullName { get; set; } = string.Empty;

    /// <summary>Öğrencinin TC Kimlik Numarası.</summary>
    public string StudentNationalId { get; set; } = string.Empty;

    /// <summary>Öğrencinin Üniversite Öğrenci Numarası.</summary>
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>Öğrencinin bağlı olduğu fakültenin adı.</summary>
    public string FacultyName { get; set; } = string.Empty;

    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public TimesheetStatus Status { get; set; }
    public string? MakerNote { get; set; }
    public string? CheckerNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Onaylayan SKS Admin'in adı (onaylanmamışsa null).</summary>
    public string? ApprovedByUserName { get; set; }
}
