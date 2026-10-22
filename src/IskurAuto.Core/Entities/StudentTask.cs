using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>Öğrenciye atanan görev. Başlık, açıklama ve tarihler atandıktan sonra değişmez.</summary>
public class StudentTask : BaseEntity
{
    public int StudentProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly AssignedDate { get; set; }

    public DateOnly DueDate { get; set; }

    public int AssignedByUserId { get; set; }

    public StudentTaskStatus Status { get; set; } = StudentTaskStatus.Open;

    public DateTime? StatusChangedAt { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
    public ICollection<TaskEvaluation> Evaluations { get; set; } = new List<TaskEvaluation>();
}
