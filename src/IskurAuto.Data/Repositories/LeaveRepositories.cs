using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class LeaveRepository : GenericRepository<LeaveRequest>, ILeaveRepository
{
    public LeaveRepository(IskurAutoDbContext context) : base(context) { }

    public Task<LeaveRequest?> GetDetailAsync(int id, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<LeaveRequest>> SearchAsync(LeaveSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking().AsQueryable();

        if (criteria.Year is int year)
            query = query.Where(l => l.StartDate >= new DateOnly(year, 1, 1) && l.StartDate <= new DateOnly(year, 12, 31));
        if (criteria.StudentId is int studentId)
            query = query.Where(l => l.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(l => l.StudentProfile.DepartmentId == departmentId);
        if (criteria.Status is { } status)
            query = query.Where(l => l.Status == status);
        if (criteria.AdvisorUserId is int advisorId)
            query = query.Where(l => l.StudentProfile.AdvisorAssignments
                .Any(a => a.AdvisorUserId == advisorId && a.EndDate == null));

        return await query.OrderByDescending(l => l.StartDate).ThenByDescending(l => l.Id).ToListAsync(ct);
    }

    public Task<bool> HasOverlapAsync(int studentId, DateOnly start, DateOnly end, int? exceptId = null, CancellationToken ct = default)
        => Context.LeaveRequests.AnyAsync(l => l.StudentProfileId == studentId
            && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved)
            && l.StartDate <= end && l.EndDate >= start
            && (exceptId == null || l.Id != exceptId), ct);

    private IQueryable<LeaveRequest> WithDetails()
        => Context.LeaveRequests
            .Include(l => l.ReviewedByUser)
            .Include(l => l.StudentProfile).ThenInclude(s => s.User)
            .Include(l => l.StudentProfile).ThenInclude(s => s.Department)
            .Include(l => l.StudentProfile).ThenInclude(s => s.AdvisorAssignments);
}

public class AbsenceRepository : GenericRepository<Absence>, IAbsenceRepository
{
    public AbsenceRepository(IskurAutoDbContext context) : base(context) { }

    public Task<Absence?> GetDetailAsync(int id, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Absence>> SearchAsync(AbsenceSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking().AsQueryable();

        if (criteria.Year is int year && criteria.Month is int month)
        {
            var first = new DateOnly(year, month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            query = query.Where(a => a.Date >= first && a.Date <= last);
        }
        else if (criteria.Year is int onlyYear)
        {
            query = query.Where(a => a.Date >= new DateOnly(onlyYear, 1, 1) && a.Date <= new DateOnly(onlyYear, 12, 31));
        }
        if (criteria.StudentId is int studentId)
            query = query.Where(a => a.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(a => a.StudentProfile.DepartmentId == departmentId);
        if (criteria.AdvisorUserId is int advisorId)
            query = query.Where(a => a.StudentProfile.AdvisorAssignments
                .Any(x => x.AdvisorUserId == advisorId && x.EndDate == null));

        return await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id).ToListAsync(ct);
    }

    public Task<bool> ActiveExistsAsync(int studentId, DateOnly date, CancellationToken ct = default)
        => Context.Absences.AnyAsync(a => a.StudentProfileId == studentId && a.Date == date && a.RevokedAt == null, ct);

    private IQueryable<Absence> WithDetails()
        => Context.Absences
            .Include(a => a.RecordedByUser)
            .Include(a => a.RevokedByUser)
            .Include(a => a.StudentProfile).ThenInclude(s => s.User)
            .Include(a => a.StudentProfile).ThenInclude(s => s.Department)
            .Include(a => a.StudentProfile).ThenInclude(s => s.AdvisorAssignments);
}

public class SystemSettingRepository : GenericRepository<SystemSetting>, ISystemSettingRepository
{
    public SystemSettingRepository(IskurAutoDbContext context) : base(context) { }

    public async Task<IReadOnlyList<SystemSetting>> GetByPrefixAsync(string prefix, CancellationToken ct = default)
        => await Context.SystemSettings.Where(s => s.Key.StartsWith(prefix)).ToListAsync(ct);
}
