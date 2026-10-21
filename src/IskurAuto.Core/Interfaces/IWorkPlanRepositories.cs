using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<IReadOnlyList<Holiday>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<bool> DateExistsAsync(DateOnly date, CancellationToken ct = default);
}

public interface IExamPeriodRepository : IRepository<ExamPeriod>
{
    Task<IReadOnlyList<ExamPeriod>> GetOverlappingAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface IStudentCourseDayRepository : IRepository<StudentCourseDay>
{
    Task<IReadOnlyList<StudentCourseDay>> GetByStudentAsync(int studentId, CancellationToken ct = default);
}

public interface IWorkPlanRepository : IRepository<MonthlyWorkPlan>
{
    /// <summary>Günleriyle birlikte, izlenen halde döner.</summary>
    Task<MonthlyWorkPlan?> GetWithDaysAsync(int studentId, int year, int month, CancellationToken ct = default);

    Task<MonthlyWorkPlan?> GetDetailAsync(int planId, CancellationToken ct = default);

    Task<IReadOnlyList<MonthlyWorkPlan>> ListByStudentAsync(int studentId, CancellationToken ct = default);

    Task<IReadOnlyList<MonthlyWorkPlan>> SearchAsync(WorkPlanSearchCriteria criteria, CancellationToken ct = default);

    void RemoveDays(IEnumerable<WorkPlanDay> days);
}
