using System.ComponentModel.DataAnnotations;

namespace IskurAuto.Service.DTOs;

/// <summary>Liste satırı. Kişisel veriler her zaman maskelidir.</summary>
public class StudentSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string MaskedNationalId { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int RegistrationYear { get; set; }
    public bool IsActive { get; set; }
    public string? CurrentUnitName { get; set; }
    public string? CurrentAdvisorName { get; set; }
}

public class StudentDetailDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>true ise NationalId ve Iban maskelidir (isteği yapanın ham veriyi görme yetkisi yok).</summary>
    public bool IsPersonalDataMasked { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int RegistrationYear { get; set; }
    public bool IsActive { get; set; }

    public UnitAssignmentDto? CurrentUnit { get; set; }
    public AdvisorAssignmentDto? CurrentAdvisor { get; set; }

    // Geçmiş (belge madde 9): en yeni kayıt başta.
    public List<ParticipationDto> Participations { get; set; } = new();
    public List<UnitAssignmentDto> UnitHistory { get; set; } = new();
    public List<AdvisorAssignmentDto> AdvisorHistory { get; set; } = new();
}

public class ParticipationDto
{
    public int Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? EndReason { get; set; }
    public bool IsOpen { get; set; }
}

public class UnitAssignmentDto
{
    public int Id { get; set; }
    public int WorkUnitId { get; set; }
    public string WorkUnitName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class AdvisorAssignmentDto
{
    public int Id { get; set; }
    public int AdvisorUserId { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

/// <summary>Liste/arama sorgusu (belge madde 10). Hepsi isteğe bağlıdır.</summary>
public class StudentSearchQuery
{
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public int? WorkUnitId { get; set; }
    public int? RegistrationYear { get; set; }
    public bool? IsActive { get; set; }

    [StringLength(100)]
    public string? Text { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Öğrencinin kendi düzenleyebildiği alanlar (belge 2.4: "izin verilen alanları düzenleyebilecektir").</summary>
public class UpdateMyProfileRequest
{
    [Required, StringLength(25)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Iban { get; set; } = string.Empty;
}

public class EndParticipationRequest
{
    /// <summary>Boş bırakılırsa bugün.</summary>
    public DateOnly? EndDate { get; set; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class StartParticipationRequest
{
    /// <summary>Boş bırakılırsa bugün.</summary>
    public DateOnly? StartDate { get; set; }
}

public class AssignUnitRequest
{
    [Range(1, int.MaxValue)]
    public int WorkUnitId { get; set; }
}

public class AssignAdvisorRequest
{
    [Range(1, int.MaxValue)]
    public int AdvisorUserId { get; set; }
}
