using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Models;

namespace IskurAuto.Service.WorkPlans;

public enum DayBlock
{
    None,
    Weekend,
    Holiday,
    OutsideProgram
}

public sealed record ProgramPeriod(DateOnly Start, DateOnly? End);

public sealed record PlannedDay(DateOnly Date, string? Note);

/// <summary>Bir ayın takvim bilgisi: tatiller, sınav dönemleri, öğrencinin ders günleri ve program dönemleri.</summary>
public sealed class PlanMonth
{
    private readonly HashSet<DateOnly> _holidays;
    private readonly IReadOnlyList<ExamPeriod> _examPeriods;
    private readonly Dictionary<DayOfWeek, CourseSession> _lessonDays;
    private readonly IReadOnlyList<ProgramPeriod> _programPeriods;

    public PlanMonth(int year, int month, IEnumerable<DateOnly> holidays, IEnumerable<ExamPeriod> examPeriods,
        IEnumerable<StudentCourseDay> lessonDays, IEnumerable<ProgramPeriod> programPeriods)
    {
        Year = year;
        Month = month;
        First = new DateOnly(year, month, 1);
        Last = First.AddMonths(1).AddDays(-1);
        _holidays = holidays.ToHashSet();
        _examPeriods = examPeriods.ToList();
        _lessonDays = lessonDays.ToDictionary(c => c.DayOfWeek, c => c.Session);
        _programPeriods = programPeriods.ToList();
    }

    public int Year { get; }
    public int Month { get; }
    public DateOnly First { get; }
    public DateOnly Last { get; }

    public IEnumerable<DateOnly> Days
    {
        get
        {
            for (var day = First; day <= Last; day = day.AddDays(1))
                yield return day;
        }
    }

    public DayBlock BlockOf(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return DayBlock.Weekend;
        if (_holidays.Contains(date)) return DayBlock.Holiday;
        if (!_programPeriods.Any(p => date >= p.Start && (p.End is null || date <= p.End))) return DayBlock.OutsideProgram;
        return DayBlock.None;
    }

    public bool IsLessonDay(DateOnly date) => _lessonDays.ContainsKey(date.DayOfWeek);

    public CourseSession? LessonSession(DateOnly date)
        => _lessonDays.TryGetValue(date.DayOfWeek, out var session) ? session : null;

    public bool IsExamDay(DateOnly date) => _examPeriods.Any(e => date >= e.StartDate && date <= e.EndDate);

    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}

public static class WorkPlanRules
{
    public static IReadOnlyList<string> Validate(PlanMonth month, IReadOnlyCollection<PlannedDay> days, WorkPlanSettings settings)
    {
        var errors = new List<string>();

        CheckDays(month, days, errors);
        CheckWeeks(month, days, settings, errors);

        return errors;
    }

    private static void CheckDays(PlanMonth month, IReadOnlyCollection<PlannedDay> days, List<string> errors)
    {
        foreach (var duplicate in days.GroupBy(d => d.Date).Where(g => g.Count() > 1))
            errors.Add($"{Format(duplicate.Key)} tarihi birden fazla kez seçilmiş.");

        foreach (var day in days)
        {
            if (day.Date < month.First || day.Date > month.Last)
            {
                errors.Add($"{Format(day.Date)} bu ayın içinde değil.");
                continue;
            }

            var reason = month.BlockOf(day.Date) switch
            {
                DayBlock.Weekend => "hafta sonu olduğu için",
                DayBlock.Holiday => "resmî tatil olduğu için",
                DayBlock.OutsideProgram => "programa katılım döneminin dışında kaldığı için",
                _ => null
            };

            if (reason is not null)
                errors.Add($"{Format(day.Date)} {reason} çalışma günü olarak seçilemez.");
            else if (month.IsLessonDay(day.Date) && string.IsNullOrWhiteSpace(day.Note))
                errors.Add($"{Format(day.Date)} dersli bir gün; seçmek için açıklama yazmalısınız.");
        }
    }

    private static void CheckWeeks(PlanMonth month, IReadOnlyCollection<PlannedDay> days, WorkPlanSettings settings, List<string> errors)
    {
        var selectedByWeek = days
            .Where(d => d.Date >= month.First && d.Date <= month.Last)
            .GroupBy(d => PlanMonth.WeekStart(d.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var week in month.Days.GroupBy(PlanMonth.WeekStart))
        {
            selectedByWeek.TryGetValue(week.Key, out var selected);
            var label = $"{Format(week.Key)} haftası";

            if (selected > settings.MaxDaysPerWeek)
                errors.Add($"{label}: en fazla {settings.MaxDaysPerWeek} gün seçilebilir.");

            var belongsToMonth = IsInMonth(month, week.Key.AddDays(3));
            var hasSelectableDay = week.Any(d => month.BlockOf(d) == DayBlock.None);
            var isExamWeek = week.Any(month.IsExamDay);

            if (belongsToMonth && hasSelectableDay && !isExamWeek && selected < settings.MinDaysPerWeek)
                errors.Add($"{label}: en az {settings.MinDaysPerWeek} gün seçilmelidir.");
        }
    }

    // Hafta, perşembe gününün düştüğü aya sayılır; ay sınırındaki yarım hafta iki planda birden zorunlu olmaz.
    private static bool IsInMonth(PlanMonth month, DateOnly date) => date >= month.First && date <= month.Last;

    private static string Format(DateOnly date) => date.ToString("dd.MM.yyyy");
}
