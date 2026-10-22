using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

public class AssignTaskRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateOnly DueDate { get; set; }
}

public class EvaluateTaskRequest
{
    [StringLength(2000)]
    public string? Note { get; set; }

    /// <summary>Doluysa görevin durumu bu değere getirilir.</summary>
    public StudentTaskStatus? NewStatus { get; set; }
}

public class TaskEvaluationDto
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public StudentTaskStatus? NewStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaskDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly AssignedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
    public StudentTaskStatus Status { get; set; }
    public DateTime? StatusChangedAt { get; set; }

    /// <summary>Hâlâ açık ve hedef tarihi geçmiş.</summary>
    public bool IsOverdue { get; set; }

    public int EvaluationCount { get; set; }

    /// <summary>Yalnızca tekil görev yanıtında dolu; en yeni sonda.</summary>
    public IReadOnlyList<TaskEvaluationDto> Evaluations { get; set; } = Array.Empty<TaskEvaluationDto>();
}

public class TaskQuery
{
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public StudentTaskStatus? Status { get; set; }
    public int? Year { get; set; }
    public bool OverdueOnly { get; set; }
}

public class SendAbsenceNoticeRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    public DateOnly Date { get; set; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Message { get; set; } = string.Empty;
}

public class AbsenceNoticeDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AdvisorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ReadByName { get; set; }
}

public class AbsenceNoticeQuery
{
    public int? StudentId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public bool UnreadOnly { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}
