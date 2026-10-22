using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Repositories;

public class StudentRepository : GenericRepository<StudentProfile>, IStudentRepository
{
    public StudentRepository(IskurAutoDbContext context) : base(context) { }

    public Task<StudentProfile?> GetDetailAsync(int studentId, CancellationToken ct = default)
        => WithDetails(Context.StudentProfiles).FirstOrDefaultAsync(s => s.Id == studentId, ct);

    public Task<StudentProfile?> GetDetailByUserIdAsync(int userId, CancellationToken ct = default)
        => WithDetails(Context.StudentProfiles).FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct = default)
        => Context.StudentProfiles.AnyAsync(s => s.NationalId == nationalId, ct);

    public Task<bool> StudentNumberExistsAsync(string studentNumber, CancellationToken ct = default)
        => Context.StudentProfiles.AnyAsync(s => s.StudentNumber == studentNumber, ct);

    public async Task<PagedResult<StudentProfile>> SearchAsync(
        StudentSearchCriteria criteria, PageRequest page, CancellationToken ct = default)
    {
        var query = Context.StudentProfiles.AsNoTracking().AsQueryable();

        query = ApplyFilters(query, criteria);

        var total = await query.CountAsync(ct);

        // AsSplitQuery: birden çok koleksiyon Include edilince tek dev JOIN yerine
        // ayrı küçük sorgular çalıştırır (kartezyen patlamasını önler).
        var items = await query
            .Include(s => s.User)
            .Include(s => s.Department)
            .Include(s => s.Participations)
            .Include(s => s.UnitAssignments).ThenInclude(a => a.WorkUnit)
            .Include(s => s.AdvisorAssignments).ThenInclude(a => a.AdvisorUser)
            .AsSplitQuery()
            .OrderBy(s => s.User.FullName).ThenBy(s => s.Id)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResult<StudentProfile>
        {
            Items = items,
            TotalCount = total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    public async Task<IReadOnlyList<StudentProfile>> ListAsync(StudentSearchCriteria criteria, CancellationToken ct = default)
        => await ApplyFilters(Context.StudentProfiles.AsNoTracking(), criteria)
            .Include(s => s.User)
            .Include(s => s.Department)
            .Include(s => s.Participations)
            .Include(s => s.UnitAssignments).ThenInclude(a => a.WorkUnit)
            .Include(s => s.AdvisorAssignments).ThenInclude(a => a.AdvisorUser)
            .AsSplitQuery()
            .OrderBy(s => s.User.FullName).ThenBy(s => s.Id)
            .ToListAsync(ct);

    private static IQueryable<StudentProfile> ApplyFilters(IQueryable<StudentProfile> query, StudentSearchCriteria criteria)
    {
        if (criteria.StudentId.HasValue)
            query = query.Where(s => s.Id == criteria.StudentId.Value);

        if (criteria.DepartmentId.HasValue)
            query = query.Where(s => s.DepartmentId == criteria.DepartmentId.Value);

        if (criteria.RegistrationYear.HasValue)
            query = query.Where(s => s.RegistrationYear == criteria.RegistrationYear.Value);

        // "Şu anki" danışman/birim = bitiş tarihi boş olan atama.
        if (criteria.AdvisorUserId.HasValue)
            query = query.Where(s => s.AdvisorAssignments.Any(a =>
                a.EndDate == null && a.AdvisorUserId == criteria.AdvisorUserId.Value));

        if (criteria.WorkUnitId.HasValue)
            query = query.Where(s => s.UnitAssignments.Any(a =>
                a.EndDate == null && a.WorkUnitId == criteria.WorkUnitId.Value));

        // Aktif = açık katılım dönemi var.
        if (criteria.IsActive == true)
            query = query.Where(s => s.Participations.Any(p => p.EndDate == null));
        else if (criteria.IsActive == false)
            query = query.Where(s => !s.Participations.Any(p => p.EndDate == null));

        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            // Kullanıcının yazdığı % ve _ karakterleri joker sayılmasın diye kaçışlanır.
            var pattern = $"%{EscapeLike(criteria.Text.Trim())}%";
            query = query.Where(s =>
                EF.Functions.Like(s.User.FullName, pattern, "\\") ||
                EF.Functions.Like(s.StudentNumber, pattern, "\\") ||
                EF.Functions.Like(s.User.Email, pattern, "\\"));
        }

        return query;
    }

    private static IQueryable<StudentProfile> WithDetails(IQueryable<StudentProfile> source)
        => source
            .Include(s => s.User)
            .Include(s => s.Department)
            .Include(s => s.Participations)
            .Include(s => s.UnitAssignments).ThenInclude(a => a.WorkUnit)
            .Include(s => s.AdvisorAssignments).ThenInclude(a => a.AdvisorUser)
            .AsSplitQuery();

    private static string EscapeLike(string input)
        => input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
