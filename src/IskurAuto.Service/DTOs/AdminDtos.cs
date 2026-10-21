using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class DepartmentUpsertRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class WorkUnitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class WorkUnitUpsertRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Personel (Fakülte yönetimi / Bölüm Başkanı / Danışman) hesabı oluşturma.</summary>
public class CreateStaffUserRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [StringLength(25)]
    public string? PhoneNumber { get; set; }

    /// <summary>Başlangıç parolası. Kullanıcı sonradan değiştirebilir.</summary>
    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    /// <summary>FacultyAdmin | DepartmentHead | Advisor (Student bu yoldan oluşturulamaz).</summary>
    public UserRole Role { get; set; }

    /// <summary>DepartmentHead ve Advisor için zorunlu.</summary>
    public int? DepartmentId { get; set; }
}

public class SetUserActiveRequest
{
    public bool IsActive { get; set; }
}

public class UserSearchQuery
{
    public UserRole? Role { get; set; }
    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

public class AuditLogDto
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class AuditLogQuery
{
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
