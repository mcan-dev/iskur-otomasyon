using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class AttendanceSessionRepository : GenericRepository<AttendanceSession>, IAttendanceSessionRepository
{
    public AttendanceSessionRepository(IskurAutoDbContext context) : base(context) { }

    public Task<AttendanceSession?> GetDetailAsync(int id, CancellationToken ct = default)
        => Context.AttendanceSessions
            .Include(s => s.Department)
            .Include(s => s.CreatedByUser)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<AttendanceSession>> ListAsync(DateOnly from, DateOnly to, int? departmentId, CancellationToken ct = default)
    {
        var query = Context.AttendanceSessions.AsNoTracking()
            .Include(s => s.Department)
            .Include(s => s.CreatedByUser)
            .Where(s => s.Date >= from && s.Date <= to);

        if (departmentId is int id)
            query = query.Where(s => s.DepartmentId == null || s.DepartmentId == id);

        return await query.OrderByDescending(s => s.Date).ThenBy(s => s.StartTime).ToListAsync(ct);
    }

    public Task<bool> HasOverlapAsync(DateOnly date, TimeOnly start, TimeOnly end, int? departmentId, CancellationToken ct = default)
        => Context.AttendanceSessions.AnyAsync(s => s.Date == date
            && s.ClosedAt == null
            && s.StartTime < end && s.EndTime > start
            && (departmentId == null || s.DepartmentId == null || s.DepartmentId == departmentId), ct);
}

public class AttendanceRecordRepository : GenericRepository<AttendanceRecord>, IAttendanceRecordRepository
{
    public AttendanceRecordRepository(IskurAutoDbContext context) : base(context) { }

    public Task<AttendanceRecord?> GetDetailAsync(int id, CancellationToken ct = default)
        => WithDetails().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<AttendanceRecord>> SearchAsync(AttendanceSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = WithDetails().AsNoTracking().AsQueryable();

        if (criteria.Date is DateOnly date)
        {
            query = query.Where(r => r.Date == date);
        }
        else if (criteria.Year is int year && criteria.Month is int month)
        {
            var first = new DateOnly(year, month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            query = query.Where(r => r.Date >= first && r.Date <= last);
        }
        else if (criteria.Year is int onlyYear)
        {
            query = query.Where(r => r.Date >= new DateOnly(onlyYear, 1, 1) && r.Date <= new DateOnly(onlyYear, 12, 31));
        }

        if (criteria.SessionId is int sessionId)
            query = query.Where(r => r.AttendanceSessionId == sessionId);
        if (criteria.StudentId is int studentId)
            query = query.Where(r => r.StudentProfileId == studentId);
        if (criteria.DepartmentId is int departmentId)
            query = query.Where(r => r.StudentProfile.DepartmentId == departmentId);
        if (criteria.AdvisorUserId is int advisorId)
            query = query.Where(r => r.StudentProfile.AdvisorAssignments
                .Any(a => a.AdvisorUserId == advisorId && a.EndDate == null));
        if (!criteria.IncludeVoided)
            query = query.Where(r => r.VoidedAt == null);

        return await query.OrderByDescending(r => r.CheckedInAt).ThenByDescending(r => r.Id).ToListAsync(ct);
    }

    public Task<bool> ActiveExistsAsync(int studentId, int sessionId, CancellationToken ct = default)
        => Context.AttendanceRecords.AnyAsync(r => r.StudentProfileId == studentId
            && r.AttendanceSessionId == sessionId && r.VoidedAt == null, ct);

    private IQueryable<AttendanceRecord> WithDetails()
        => Context.AttendanceRecords
            .Include(r => r.AttendanceSession)
            .Include(r => r.CreatedByUser)
            .Include(r => r.VoidedByUser)
            .Include(r => r.StudentProfile).ThenInclude(s => s.User)
            .Include(r => r.StudentProfile).ThenInclude(s => s.Department)
            .Include(r => r.StudentProfile).ThenInclude(s => s.AdvisorAssignments);
}
