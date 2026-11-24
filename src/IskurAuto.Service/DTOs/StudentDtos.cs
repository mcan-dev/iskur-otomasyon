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

    /// <summary>Öğrenciye özel izin hakkı (gün); boşsa genel hak geçerlidir.</summary>
    public int? LeaveDayAllowance { get; set; }

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

    /// <summary>Hesap sahibinin adı soyadı; öğrencinin adıyla aynı olmalıdır (IBAN öğrencinin kendisine ait olmalı).</summary>
    [Required, StringLength(150)]
    public string IbanHolderName { get; set; } = string.Empty;

    /// <summary>Öğrencinin "bu IBAN bana ait, şubeli bir bankadadır" beyanı.</summary>
    public bool ConfirmOwnIban { get; set; }
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

public class SetLeaveAllowanceRequest
{
    /// <summary>Öğrenciye özel yıllık izin hakkı (gün). Boş bırakılırsa genel hak geçerli olur.</summary>
    [Range(0, 365)]
    public int? Days { get; set; }
}

public class AssignUnitRequest
{
    [Range(1, int.MaxValue)]
    public int WorkUnitId { get; set; }
}

/// <summary>Sorumlu personel ataması; personel Id'si ya da sicil numarasıyla seçilir.</summary>
public class AssignAdvisorRequest
{
    public int? AdvisorUserId { get; set; }

    /// <summary>Sorumlu personelin sicil numarası (Id yerine).</summary>
    [StringLength(20)]
    public string? AdvisorRegistryNumber { get; set; }
}

/// <summary>Birden çok öğrenciyi tek işlemde birime ve/veya sorumlu personele atama.</summary>
public class BulkAssignRequest
{
    [Required, MinLength(1)]
    public List<int> StudentIds { get; set; } = new();

    public int? WorkUnitId { get; set; }

    public int? AdvisorUserId { get; set; }

    /// <summary>Sorumlu personelin sicil numarası (Id yerine).</summary>
    [StringLength(20)]
    public string? AdvisorRegistryNumber { get; set; }
}

public class BulkAssignResultDto
{
    public int Assigned { get; set; }
    public List<BulkAssignErrorDto> Errors { get; set; } = new();
}

public class BulkAssignErrorDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Bilgi işlemden gelen öğrenci bilgileriyle öğrenci hesabı açma (yalnızca fakülte sekreterliği).
/// Öğrenci sisteme öğrenci e-postası ve OBİS şifresiyle girer; kendi kendine kayıt olamaz.
/// </summary>
public class CreateStudentRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Üniversite öğrenci e-postası (giriş adı).</summary>
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Bilgi işlemden alınan OBİS şifresi (yalnızca özeti saklanır).</summary>
    [Required, StringLength(128)]
    public string ObisPassword { get; set; } = string.Empty;

    [Required, StringLength(25)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(11, MinimumLength = 11)]
    public string NationalId { get; set; } = string.Empty;

    [Required, StringLength(9, MinimumLength = 3)]
    public string StudentNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Range(2000, 2100)]
    public int RegistrationYear { get; set; }
}

/// <summary>Sekreterliğin öğrenci bilgilerini düzeltmesi (süper yetki). Boş bırakılan şifre ve IBAN değişmez.</summary>
public class UpdateStudentRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(25)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(11, MinimumLength = 11)]
    public string NationalId { get; set; } = string.Empty;

    [Required, StringLength(9, MinimumLength = 3)]
    public string StudentNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Range(2000, 2100)]
    public int RegistrationYear { get; set; }

    [StringLength(40)]
    public string? Iban { get; set; }

    [StringLength(128)]
    public string? NewObisPassword { get; set; }
}
