using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface IStudentTaskRepository : IRepository<StudentTask>
{
    /// <summary>Değerlendirmeleriyle birlikte, izlenen halde döner.</summary>
    Task<StudentTask?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<StudentTask>> SearchAsync(StudentTaskSearchCriteria criteria, CancellationToken ct = default);
}

public interface IAbsenceNoticeRepository : IRepository<AbsenceNotice>
{
    Task<AbsenceNotice?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<AbsenceNotice>> SearchAsync(AbsenceNoticeSearchCriteria criteria, CancellationToken ct = default);

    Task<bool> ExistsAsync(int advisorUserId, int studentId, DateOnly date, CancellationToken ct = default);
}
