using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.WorkPlans;

namespace IskurAuto.Service.Mapping;

public static class WorkPlanMappers
{
    public static WorkPlanDto ToDto(this MonthlyWorkPlan plan, WorkPlanSettings settings)
    {
        var days = plan.Days.OrderBy(d => d.Date).ToList();

        return new WorkPlanDto
        {
            Id = plan.Id,
            StudentId = plan.StudentProfileId,
            StudentName = plan.StudentProfile.User.FullName,
            DepartmentName = plan.StudentProfile.Department.Name,
            Year = plan.Year,
            Month = plan.Month,
            Status = plan.Status,
            StudentNote = plan.StudentNote,
            LastSavedAt = plan.LastSavedAt,
            ReviewedByName = plan.ReviewedByUser?.FullName,
            ReviewedAt = plan.ReviewedAt,
            ReviewNote = plan.ReviewNote,
            PlannedDays = days.Count,
            PlannedHours = days.Count * settings.DailyHours,
            Days = days.Select(d => new WorkPlanDayDto
            {
                Date = d.Date,
                IsLessonDay = d.IsLessonDay,
                Note = d.Note,
                Hours = settings.DailyHours
            }).ToList()
        };
    }

    public static HolidayDto ToDto(this Holiday h) => new() { Id = h.Id, Date = h.Date, Name = h.Name };

    public static ExamPeriodDto ToDto(this ExamPeriod e)
        => new() { Id = e.Id, Name = e.Name, StartDate = e.StartDate, EndDate = e.EndDate };

    public static List<CalendarDayDto> ToCalendar(this PlanMonth month, MonthlyWorkPlan? plan)
    {
        var selected = plan?.Days.ToDictionary(d => d.Date) ?? new Dictionary<DateOnly, WorkPlanDay>();

        return month.Days.Select(date =>
        {
            selected.TryGetValue(date, out var chosen);
            var block = month.BlockOf(date);
            var lesson = month.IsLessonDay(date);

            var (color, label) = block switch
            {
                DayBlock.Weekend => (CalendarColor.Red, "Hafta sonu"),
                DayBlock.Holiday => (CalendarColor.Red, "Resmî tatil"),
                DayBlock.OutsideProgram => (CalendarColor.Red, "Program dışı"),
                _ when chosen is not null && lesson => (CalendarColor.Yellow, "Seçildi, dersli gün"),
                _ when chosen is not null => (CalendarColor.Green, "Seçildi"),
                _ when lesson => (CalendarColor.Yellow, "Dersli gün, seçilirse açıklama gerekir"),
                _ => (CalendarColor.Gray, "Boş gün, henüz seçilmedi")
            };

            return new CalendarDayDto
            {
                Date = date,
                Color = color,
                Label = label,
                IsSelectable = block == DayBlock.None,
                IsSelected = chosen is not null,
                IsLessonDay = lesson,
                LessonSession = month.LessonSession(date),
                IsExamPeriod = month.IsExamDay(date),
                Note = chosen?.Note
            };
        }).ToList();
    }

}
