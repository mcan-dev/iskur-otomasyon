using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class HolidayRepository : GenericRepository<Holiday>, IHolidayRepository
{
    public HolidayRepository(IskurAutoDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Holiday>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => await Context.Holidays.AsNoTracking()
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .ToListAsync(ct);

    public Task<bool> DateExistsAsync(DateOnly date, CancellationToken ct = default)
        => Context.Holidays.AnyAsync(h => h.Date == date, ct);
}

public class ExamPeriodRepository : GenericRepository<ExamPeriod>, IExamPeriodRepository
{
    public ExamPeriodRepository(IskurAutoDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ExamPeriod>> GetOverlappingAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => await Context.ExamPeriods.AsNoTracking()
            .Where(e => e.StartDate <= to && e.EndDate >= from)
            .OrderBy(e => e.StartDate)
            .ToListAsync(ct);
}

public class StudentCourseDayRepository : GenericRepository<StudentCourseDay>, IStudentCourseDayRepository
{
    public StudentCourseDayRepository(IskurAutoDbContext context) : base(context) { }

    public async Task<IReadOnlyList<StudentCourseDay>> GetByStudentAsync(int studentId, CancellationToken ct = default)
        => await Context.StudentCourseDays
            .Where(c => c.StudentProfileId == studentId)
            .ToListAsync(ct);
}

public class WorkPlanRepository : GenericRepository<MonthlyWorkPlan>, IWorkPlanRepository
{
    public WorkPlanRepository(IskurAutoDbContext context) : base(context) { }

    public Task<MonthlyWorkPlan?> GetWithDaysAsync(int studentId, int year, int month, CancellationToken ct = default)
        => WithDetails()
            .FirstOrDefaultAsync(p => p.StudentProfileId == studentId && p.Year == year && p.Month == month, ct);

    public Task<MonthlyWorkPlan?> GetDetailAsync(int planId, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<IReadOnlyList<MonthlyWorkPlan>> ListByStudentAsync(int studentId, CancellationToken ct = default)
        => await WithDetails().AsNoTracking()
            .Where(p => p.StudentProfileId == studentId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MonthlyWorkPlan>> SearchAsync(WorkPlanSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking()
            .Where(p => p.Year == criteria.Year && p.Month == criteria.Month);

        if (criteria.StudentId is int studentId)
            query = query.Where(p => p.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(p => p.StudentProfile.DepartmentId == departmentId);
        if (criteria.Status is { } status)
            query = query.Where(p => p.Status == status);
        if (criteria.AdvisorUserId is int advisorId)
            query = query.Where(p => p.StudentProfile.AdvisorAssignments
                .Any(a => a.AdvisorUserId == advisorId && a.EndDate == null));

        return await query.OrderBy(p => p.StudentProfile.User.FullName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MonthlyWorkPlan>> GetPlansInRangeAsync(int studentId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromKey = from.Year * 12 + from.Month;
        var toKey = to.Year * 12 + to.Month;

        return await Context.WorkPlans.AsNoTracking()
            .Include(p => p.Days)
            .Where(p => p.StudentProfileId == studentId
                        && p.Year * 12 + p.Month >= fromKey && p.Year * 12 + p.Month <= toKey)
            .ToListAsync(ct);
    }

    public void RemoveDays(IEnumerable<WorkPlanDay> days) => Context.WorkPlanDays.RemoveRange(days);

    private IQueryable<MonthlyWorkPlan> WithDetails()
        => Context.WorkPlans
            .Include(p => p.Days)
            .Include(p => p.ReviewedByUser)
            .Include(p => p.StudentProfile).ThenInclude(s => s.User)
            .Include(p => p.StudentProfile).ThenInclude(s => s.Department)
            .Include(p => p.StudentProfile).ThenInclude(s => s.AdvisorAssignments)
            .Include(p => p.StudentProfile).ThenInclude(s => s.Participations);
}
