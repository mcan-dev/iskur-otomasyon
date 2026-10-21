using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;

namespace IskurAuto.Service.Implementations;

public class CourseDayService : ICourseDayService
{
    private readonly IStudentRepository _students;
    private readonly IStudentCourseDayRepository _courseDays;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public CourseDayService(IStudentRepository students, IStudentCourseDayRepository courseDays,
        IUnitOfWork uow, IAuditService audit, ICurrentUser current)
    {
        _students = students;
        _courseDays = courseDays;
        _uow = uow;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<DayOfWeek>> GetMineAsync(CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        var stored = await _courseDays.GetByStudentAsync(student.Id, ct);
        return stored.Select(c => c.DayOfWeek).OrderBy(Order).ToList();
    }

    public async Task<IReadOnlyList<DayOfWeek>> SetMineAsync(CourseDaysRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var requested = request.Days.Distinct().ToList();
        if (requested.Any(d => d is DayOfWeek.Saturday or DayOfWeek.Sunday))
            throw new DomainValidationException(nameof(request.Days), "Ders günleri hafta içi günlerden seçilmelidir.");

        var student = await LoadMineAsync(ct);
        var stored = await _courseDays.GetByStudentAsync(student.Id, ct);

        _courseDays.RemoveRange(stored.Where(c => !requested.Contains(c.DayOfWeek)).ToList());

        var missing = requested.Where(d => stored.All(c => c.DayOfWeek != d));
        await _courseDays.AddRangeAsync(missing.Select(d => new StudentCourseDay { StudentProfileId = student.Id, DayOfWeek = d }));

        await _audit.RecordAsync(AuditActions.CourseDaysUpdated, nameof(StudentProfile), student.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return requested.OrderBy(Order).ToList();
    }

    private async Task<StudentProfile> LoadMineAsync(CancellationToken ct)
        => await _students.GetDetailByUserIdAsync(_current.UserId, ct)
           ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

    // Pazartesi'den başlayan sıra
    private static int Order(DayOfWeek day) => ((int)day + 6) % 7;
}
