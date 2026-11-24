using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;

namespace IskurAuto.Service.Implementations;

/// <summary>
/// Kimin hangi gün çalıştığı (sorumlu personel, birim yetkilisi, sekreterlik) ve geçmiş yıllarda çalışanlar
/// (birim yetkilisi, sekreterlik). Kapsam rolden gelir: sorumlu personel kendi öğrencilerini, birim yetkilisi
/// kendi bölümünü, sekreterlik herkesi görür.
/// </summary>
public class WorkRosterService : IWorkRosterService
{
    private readonly IWorkPlanRepository _plans;
    private readonly IAttendanceRecordRepository _records;
    private readonly ILeaveRepository _leaves;
    private readonly IClock _clock;
    private readonly ICurrentUser _current;

    public WorkRosterService(IWorkPlanRepository plans, IAttendanceRecordRepository records, ILeaveRepository leaves,
        IClock clock, ICurrentUser current)
    {
        _plans = plans;
        _records = records;
        _leaves = leaves;
        _clock = clock;
        _current = current;
    }

    public async Task<WorkRosterDto> GetMonthAsync(RosterQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);
        EnsureValidMonth(query.Year, query.Month);

        var (departmentId, advisorId) = Scope(query.DepartmentId);

        var plans = await _plans.SearchAsync(new WorkPlanSearchCriteria
        {
            Year = query.Year,
            Month = query.Month,
            DepartmentId = departmentId,
            AdvisorUserId = advisorId,
            Status = WorkPlanStatus.Approved
        }, ct);

        var attended = (await _records.SearchAsync(new AttendanceSearchCriteria
        {
            Year = query.Year,
            Month = query.Month,
            DepartmentId = departmentId,
            AdvisorUserId = advisorId
        }, ct)).Select(r => (r.StudentProfileId, r.Date)).ToHashSet();

        var onLeave = (await _leaves.SearchAsync(new LeaveSearchCriteria
        {
            Year = query.Year,
            Month = query.Month,
            DepartmentId = departmentId,
            AdvisorUserId = advisorId,
            Status = LeaveStatus.Approved
        }, ct)).Select(l => (l.StudentProfileId, l.StartDate)).ToHashSet();

        var today = _clock.Today;
        var entries = plans
            .SelectMany(p => p.Days.Select(d => new { Plan = p, d.Date }))
            .GroupBy(x => x.Date)
            .OrderBy(g => g.Key)
            .Select(g => new WorkRosterDayDto
            {
                Date = g.Key,
                Students = g
                    .Select(x => new WorkRosterEntryDto
                    {
                        StudentId = x.Plan.StudentProfileId,
                        StudentName = x.Plan.StudentProfile.User.FullName,
                        StudentNumber = x.Plan.StudentProfile.StudentNumber,
                        DepartmentName = x.Plan.StudentProfile.Department.Name,
                        Status = onLeave.Contains((x.Plan.StudentProfileId, x.Date)) ? "OnLeave"
                            : attended.Contains((x.Plan.StudentProfileId, x.Date)) ? "Attended"
                            : x.Date < today ? "Missing"
                            : "Planned"
                    })
                    .OrderBy(e => e.StudentName)
                    .ToList()
            })
            .ToList();

        return new WorkRosterDto { Year = query.Year, Month = query.Month, Days = entries };
    }

    public async Task<YearWorkersDto> GetYearAsync(YearWorkersQuery query, CancellationToken ct = default)
    {
        // Geçmiş yıllarda kimlerin çalıştığına birim yetkilisi ve sekreterlik bakar.
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);
        if (query.Year is < 2000 or > 2100)
            throw new DomainValidationException(nameof(query.Year), "Geçerli bir yıl giriniz.");

        var (departmentId, advisorId) = Scope(query.DepartmentId);

        var planned = new Dictionary<int, (StudentProfile Student, int Days)>();
        for (var month = 1; month <= 12; month++)
        {
            var plans = await _plans.SearchAsync(new WorkPlanSearchCriteria
            {
                Year = query.Year,
                Month = month,
                DepartmentId = departmentId,
                AdvisorUserId = advisorId,
                Status = WorkPlanStatus.Approved
            }, ct);
            foreach (var plan in plans)
            {
                planned.TryGetValue(plan.StudentProfileId, out var current);
                planned[plan.StudentProfileId] = (plan.StudentProfile, current.Days + plan.Days.Count);
            }
        }

        var records = await _records.SearchAsync(new AttendanceSearchCriteria
        {
            Year = query.Year,
            DepartmentId = departmentId,
            AdvisorUserId = advisorId
        }, ct);
        var leaves = await _leaves.SearchAsync(new LeaveSearchCriteria
        {
            Year = query.Year,
            DepartmentId = departmentId,
            AdvisorUserId = advisorId,
            Status = LeaveStatus.Approved
        }, ct);

        var students = planned.Values.Select(v => v.Student)
            .Concat(records.Select(r => r.StudentProfile))
            .DistinctBy(s => s.Id);

        var rows = students
            .Select(s => new YearWorkerDto
            {
                StudentId = s.Id,
                StudentName = s.User.FullName,
                StudentNumber = s.StudentNumber,
                DepartmentName = s.Department.Name,
                PlannedDays = planned.TryGetValue(s.Id, out var p) ? p.Days : 0,
                WorkedDays = records.Where(r => r.StudentProfileId == s.Id).Select(r => r.Date).Distinct().Count(),
                LeaveDays = leaves.Where(l => l.StudentProfileId == s.Id).Sum(l => l.DaysCounted)
            })
            .Where(r => r.PlannedDays > 0 || r.WorkedDays > 0)
            .OrderBy(r => r.DepartmentName).ThenBy(r => r.StudentName)
            .ToList();

        return new YearWorkersDto { Year = query.Year, Students = rows };
    }

    private (int? DepartmentId, int? AdvisorUserId) Scope(int? requestedDepartmentId) => _current.Role switch
    {
        UserRole.DepartmentHead => (_current.DepartmentId ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış."), null),
        UserRole.Advisor => (requestedDepartmentId, _current.UserId),
        _ => (requestedDepartmentId, null)
    };

    private static void EnsureValidMonth(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new DomainValidationException("Month", "Geçerli bir yıl ve ay giriniz.");
    }
}
