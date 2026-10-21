namespace IskurAuto.Core.Models;

public class WorkPlanSettings
{
    public const string SectionName = "WorkPlan";

    public int MinDaysPerWeek { get; set; } = 1;

    public int MaxDaysPerWeek { get; set; } = 3;

    /// <summary>Plan, ait olduğu ayın bu gününe kadar öğrenci tarafından düzeltilebilir.</summary>
    public int EditDeadlineDay { get; set; } = 5;

    public decimal FullDayHours { get; set; } = 8;

    public decimal HoursOf(Enums.WorkShift shift)
        => shift == Enums.WorkShift.FullDay ? FullDayHours : FullDayHours / 2;
}
