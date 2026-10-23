using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>Öğrencinin beyan ettiği, haftanın dersli günlerinden biri.</summary>
public class StudentCourseDay : BaseEntity
{
    public int StudentProfileId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>Dersin öğleden önce, öğleden sonra ya da tüm gün olduğu.</summary>
    public CourseSession Session { get; set; } = CourseSession.FullDay;

    public StudentProfile StudentProfile { get; set; } = null!;
}
