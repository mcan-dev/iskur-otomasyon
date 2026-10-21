using System.ComponentModel.DataAnnotations;
using IskurAuto.Core.Enums;

namespace IskurAuto.Service.DTOs;

public enum CalendarColor
{
    Green = 1,
    Red = 2,
    Yellow = 3,
    Gray = 4
}

public class WorkPlanDayInput
{
    public DateOnly Date { get; set; }

    public WorkShift Shift { get; set; } = WorkShift.FullDay;

    [StringLength(500)]
    public string? Note { get; set; }
}

public class SaveWorkPlanRequest
{
    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [StringLength(1000)]
    public string? StudentNote { get; set; }

    [Required]
    public List<WorkPlanDayInput> Days { get; set; } = new();
}

public class ReviewWorkPlanRequest
{
    public bool Approve { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}

public class WorkPlanDayDto
{
    public DateOnly Date { get; set; }
    public WorkShift Shift { get; set; }
    public bool IsLessonDay { get; set; }
    public string? Note { get; set; }
    public decimal Hours { get; set; }
}

public class WorkPlanDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public WorkPlanStatus Status { get; set; }
    public string? StudentNote { get; set; }
    public DateTime LastSavedAt { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public int PlannedDays { get; set; }
    public decimal PlannedHours { get; set; }
    public List<WorkPlanDayDto> Days { get; set; } = new();
}

public class WorkPlanSearchQuery
{
    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    public int? DepartmentId { get; set; }
    public int? AdvisorUserId { get; set; }
    public int? StudentId { get; set; }
    public WorkPlanStatus? Status { get; set; }
}

public class CalendarDayDto
{
    public DateOnly Date { get; set; }
    public CalendarColor Color { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsSelectable { get; set; }
    public bool IsSelected { get; set; }
    public bool IsLessonDay { get; set; }
    public bool IsExamPeriod { get; set; }
    public WorkShift? Shift { get; set; }
    public string? Note { get; set; }
}

public class WorkPlanCalendarDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public WorkPlanStatus? PlanStatus { get; set; }
    public bool CanEdit { get; set; }
    public DateOnly EditDeadline { get; set; }
    public List<CalendarDayDto> Days { get; set; } = new();
}

public class CourseDaysRequest
{
    [Required]
    public List<DayOfWeek> Days { get; set; } = new();
}

public class HolidayDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class HolidayRequest
{
    public DateOnly Date { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class ExamPeriodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class ExamPeriodRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
