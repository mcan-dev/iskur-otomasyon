using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

public class MonthlyWorkPlan : BaseEntity
{
    public int StudentProfileId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public WorkPlanStatus Status { get; set; } = WorkPlanStatus.Pending;

    public string? StudentNote { get; set; }

    public DateTime LastSavedAt { get; set; }

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<WorkPlanDay> Days { get; set; } = new List<WorkPlanDay>();
}
