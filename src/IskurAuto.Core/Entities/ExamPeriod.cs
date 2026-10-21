namespace IskurAuto.Core.Entities;

/// <summary>Vize veya final dönemi. Bu dönemle kesişen haftalarda en az 1 gün kuralı aranmaz.</summary>
public class ExamPeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
