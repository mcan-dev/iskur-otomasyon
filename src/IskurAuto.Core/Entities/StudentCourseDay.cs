namespace IskurAuto.Core.Entities;

/// <summary>Öğrencinin beyan ettiği, haftanın dersli günlerinden biri.</summary>
public class StudentCourseDay : BaseEntity
{
    public int StudentProfileId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
