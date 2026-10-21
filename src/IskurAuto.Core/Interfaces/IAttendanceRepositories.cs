using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface IAttendanceSessionRepository : IRepository<AttendanceSession>
{
    Task<AttendanceSession?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceSession>> ListAsync(DateOnly from, DateOnly to, int? departmentId, CancellationToken ct = default);

    /// <summary>Aynı günde, kapatılmamış ve aynı kapsamla (fakülte geneli ya da aynı bölüm) çakışan oturum var mı?</summary>
    Task<bool> HasOverlapAsync(DateOnly date, TimeOnly start, TimeOnly end, int? departmentId, CancellationToken ct = default);
}

public interface IAttendanceRecordRepository : IRepository<AttendanceRecord>
{
    Task<AttendanceRecord?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceRecord>> SearchAsync(AttendanceSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>İptal edilmemiş kayıt var mı?</summary>
    Task<bool> ActiveExistsAsync(int studentId, int sessionId, CancellationToken ct = default);
}
