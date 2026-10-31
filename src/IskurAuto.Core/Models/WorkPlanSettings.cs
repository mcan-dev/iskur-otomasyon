namespace IskurAuto.Core.Models;

public class WorkPlanSettings
{
    public const string SectionName = "WorkPlan";

    public int MinDaysPerWeek { get; set; } = 1;

    public int MaxDaysPerWeek { get; set; } = 3;

    /// <summary>Plan, ait olduğu ayın bu gününe kadar öğrenci tarafından düzeltilebilir.</summary>
    public int EditDeadlineDay { get; set; } = 5;

    /// <summary>Günlük çalışma süresi (saat). İŞKUR programında haftada 3 gün ve en fazla 22,5 saat çalışıldığından 7,5 saattir. Çalışılan her gün tam gündür; yarım gün yoktur.</summary>
    public decimal DailyHours { get; set; } = 7.5m;
}
