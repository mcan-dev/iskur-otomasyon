using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Models;

/// <summary>
/// Öğrenci arama kriterleri (belge madde 10: bölüm, danışman, yıl, çalışma durumu vb. filtreler).
/// Bir kriter null ise o filtre uygulanmaz. Rol kapsamı (örn. bölüm başkanı sadece kendi bölümü)
/// kullanıcı girdisiyle değil, servis katmanında zorla bu alanlara yazılır.
/// </summary>
public class StudentSearchCriteria
{
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public int? WorkUnitId { get; set; }
    public int? RegistrationYear { get; set; }

    /// <summary>true: yalnızca aktifler, false: yalnızca pasifler, null: hepsi.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Ad-soyad, öğrenci no veya e-posta içinde aranacak metin.</summary>
    public string? Text { get; set; }
}

public class UserSearchCriteria
{
    public UserRole? Role { get; set; }
    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

public class AuditLogSearchCriteria
{
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class WorkPlanSearchCriteria
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public WorkPlanStatus? Status { get; set; }
}

public class LeaveSearchCriteria
{
    public int? Year { get; set; }
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public LeaveStatus? Status { get; set; }
}

public class AbsenceSearchCriteria
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
}
