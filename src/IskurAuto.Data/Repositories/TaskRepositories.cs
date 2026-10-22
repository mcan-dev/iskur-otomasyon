using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class StudentTaskRepository : GenericRepository<StudentTask>, IStudentTaskRepository
{
    public StudentTaskRepository(IskurAutoDbContext context) : base(context) { }

    public Task<StudentTask?> GetDetailAsync(int id, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<StudentTask>> SearchAsync(StudentTaskSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking().AsQueryable();

        if (criteria.StudentId is int studentId)
            query = query.Where(t => t.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(t => t.StudentProfile.DepartmentId == departmentId);
        if (criteria.AdvisorUserId is int advisorId)
            query = query.Where(t => t.StudentProfile.AdvisorAssignments
                .Any(a => a.AdvisorUserId == advisorId && a.EndDate == null));
        if (criteria.Status is { } status)
            query = query.Where(t => t.Status == status);
        if (criteria.Year is int year)
            query = query.Where(t => t.AssignedDate >= new DateOnly(year, 1, 1) && t.AssignedDate <= new DateOnly(year, 12, 31));
        if (criteria.OverdueBefore is DateOnly today)
            query = query.Where(t => t.Status == StudentTaskStatus.Open && t.DueDate < today);

        return await query.OrderBy(t => t.DueDate).ThenBy(t => t.Id).ToListAsync(ct);
    }

    private IQueryable<StudentTask> WithDetails()
        => Context.StudentTasks
            .Include(t => t.AssignedByUser)
            .Include(t => t.Evaluations).ThenInclude(e => e.AuthorUser)
            .Include(t => t.StudentProfile).ThenInclude(s => s.User)
            .Include(t => t.StudentProfile).ThenInclude(s => s.Department)
            .Include(t => t.StudentProfile).ThenInclude(s => s.AdvisorAssignments);
}

public class AbsenceNoticeRepository : GenericRepository<AbsenceNotice>, IAbsenceNoticeRepository
{
    public AbsenceNoticeRepository(IskurAutoDbContext context) : base(context) { }

    public Task<AbsenceNotice?> GetDetailAsync(int id, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<AbsenceNotice>> SearchAsync(AbsenceNoticeSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking().AsQueryable();

        if (criteria.Year is int year && criteria.Month is int month)
        {
            var first = new DateOnly(year, month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            query = query.Where(n => n.Date >= first && n.Date <= last);
        }
        else if (criteria.Year is int onlyYear)
        {
            query = query.Where(n => n.Date >= new DateOnly(onlyYear, 1, 1) && n.Date <= new DateOnly(onlyYear, 12, 31));
        }

        if (criteria.StudentId is int studentId)
            query = query.Where(n => n.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(n => n.StudentProfile.DepartmentId == departmentId);
        if (criteria.AdvisorUserId is int advisorId)
            query = query.Where(n => n.AdvisorUserId == advisorId);
        if (criteria.UnreadOnly)
            query = query.Where(n => n.ReadAt == null);

        return await query.OrderByDescending(n => n.Date).ThenByDescending(n => n.Id).ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(int advisorUserId, int studentId, DateOnly date, CancellationToken ct = default)
        => Context.AbsenceNotices.AnyAsync(n => n.AdvisorUserId == advisorUserId
            && n.StudentProfileId == studentId && n.Date == date, ct);

    private IQueryable<AbsenceNotice> WithDetails()
        => Context.AbsenceNotices
            .Include(n => n.AdvisorUser)
            .Include(n => n.ReadByUser)
            .Include(n => n.StudentProfile).ThenInclude(s => s.User)
            .Include(n => n.StudentProfile).ThenInclude(s => s.Department);
}

public class TerminationRepository : GenericRepository<TerminationDecision>, ITerminationRepository
{
    public TerminationRepository(IskurAutoDbContext context) : base(context) { }

    public Task<TerminationDecision?> GetDetailAsync(int id, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<TerminationDecision>> SearchAsync(TerminationSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking().AsQueryable();

        if (criteria.StudentId is int studentId)
            query = query.Where(d => d.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(d => d.StudentProfile.DepartmentId == departmentId);
        if (criteria.Year is int year)
            query = query.Where(d => d.DecisionDate >= new DateOnly(year, 1, 1) && d.DecisionDate <= new DateOnly(year, 12, 31));

        return await query.OrderByDescending(d => d.DecisionDate).ThenByDescending(d => d.Id).ToListAsync(ct);
    }

    private IQueryable<TerminationDecision> WithDetails()
        => Context.TerminationDecisions
            .Include(d => d.DecidedByUser)
            .Include(d => d.StudentProfile).ThenInclude(s => s.User)
            .Include(d => d.StudentProfile).ThenInclude(s => s.Department);
}
