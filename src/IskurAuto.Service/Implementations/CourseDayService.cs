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

    public async Task<IReadOnlyList<CourseDayDto>> GetMineAsync(CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var student = await LoadMineAsync(ct);
        var stored = await _courseDays.GetByStudentAsync(student.Id, ct);
        return stored.Select(ToDto).OrderBy(c => Order(c.Day)).ToList();
    }

    public async Task<IReadOnlyList<CourseDayDto>> SetMineAsync(CourseDaysRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);

        var requested = request.Days.GroupBy(d => d.Day).Select(g => g.Last()).ToList();
        if (requested.Any(d => d.Day is DayOfWeek.Saturday or DayOfWeek.Sunday))
            throw new DomainValidationException(nameof(request.Days), "Ders günleri hafta içi günlerden seçilmelidir.");
        if (requested.Any(d => !Enum.IsDefined(d.Session)))
            throw new DomainValidationException(nameof(request.Days), "Ders zamanı geçersiz.");

        var student = await LoadMineAsync(ct);
        EnsureIbanSaved(student);

        var stored = await _courseDays.GetByStudentAsync(student.Id, ct);

        _courseDays.RemoveRange(stored.Where(c => requested.All(r => r.Day != c.DayOfWeek)).ToList());

        foreach (var item in requested)
        {
            var existing = stored.FirstOrDefault(c => c.DayOfWeek == item.Day);
            if (existing is null)
                await _courseDays.AddRangeAsync(new[] { new StudentCourseDay { StudentProfileId = student.Id, DayOfWeek = item.Day, Session = item.Session } });
            else
                existing.Session = item.Session;
        }

        await _audit.RecordAsync(AuditActions.CourseDaysUpdated, nameof(StudentProfile), student.Id, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return requested.OrderBy(c => Order(c.Day)).ToList();
    }

    /// <summary>Gün seçimi, IBAN kaydedilmeden onaya gidemez.</summary>
    internal static void EnsureIbanSaved(StudentProfile student)
    {
        if (string.IsNullOrWhiteSpace(student.Iban))
            throw new DomainValidationException("Iban", "IBAN'ı giriniz. Gün seçimini kaydedebilmek için profilinizden IBAN bilgisini kaydetmelisiniz.");
    }

    private static CourseDayDto ToDto(StudentCourseDay c) => new() { Day = c.DayOfWeek, Session = c.Session };

    private async Task<StudentProfile> LoadMineAsync(CancellationToken ct)
        => await _students.GetDetailByUserIdAsync(_current.UserId, ct)
           ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

    // Pazartesi'den başlayan sıra
    private static int Order(DayOfWeek day) => ((int)day + 6) % 7;
}
