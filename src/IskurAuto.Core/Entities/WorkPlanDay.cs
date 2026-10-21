using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

public class WorkPlanDay : BaseEntity
{
    public int MonthlyWorkPlanId { get; set; }

    public DateOnly Date { get; set; }

    public WorkShift Shift { get; set; }

    /// <summary>Kayıt anında bu günün öğrencinin dersli günlerinden biri olup olmadığı.</summary>
    public bool IsLessonDay { get; set; }

    public string? Note { get; set; }

    public MonthlyWorkPlan MonthlyWorkPlan { get; set; } = null!;
}
