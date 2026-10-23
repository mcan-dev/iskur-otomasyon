using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

public class ReportQuery
{
    [Range(2000, 2100)]
    public int? Year { get; set; }

    [Range(1, 12)]
    public int? Month { get; set; }

    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public int? StudentId { get; set; }

    /// <summary>true: yalnızca aktif, false: yalnızca pasif, boş: hepsi.</summary>
    public bool? IsActive { get; set; }
}

public class RecordTerminationRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    public TerminationBasis Basis { get; set; }

    /// <summary>Boşsa bugün.</summary>
    public DateOnly? DecisionDate { get; set; }

    [Required, StringLength(800, MinimumLength = 10)]
    public string Note { get; set; } = string.Empty;
}

public class TerminationDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public TerminationBasis Basis { get; set; }
    public DateOnly DecisionDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public decimal LeaveDaysUsed { get; set; }
    public int AbsenceDays { get; set; }
    public string DecidedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TerminationQuery
{
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? Year { get; set; }
}
