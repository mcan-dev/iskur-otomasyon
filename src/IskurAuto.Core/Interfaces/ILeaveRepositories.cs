using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface ILeaveRepository : IRepository<LeaveRequest>
{
    Task<LeaveRequest?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveRequest>> SearchAsync(LeaveSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>İptal ve reddedilenler hariç, verilen aralıkla çakışan izin var mı?</summary>
    Task<bool> HasOverlapAsync(int studentId, DateOnly start, DateOnly end, int? exceptId = null, CancellationToken ct = default);
}

public interface IAbsenceRepository : IRepository<Absence>
{
    Task<Absence?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Absence>> SearchAsync(AbsenceSearchCriteria criteria, CancellationToken ct = default);

    Task<bool> ActiveExistsAsync(int studentId, DateOnly date, CancellationToken ct = default);
}

public interface ISystemSettingRepository : IRepository<SystemSetting>
{
    Task<IReadOnlyList<SystemSetting>> GetByPrefixAsync(string prefix, CancellationToken ct = default);
}

/// <summary>Yüklenen belgelerin (rapor, dilekçe vb.) saklandığı yer. Uygulama: API katmanı.</summary>
public interface IDocumentStore
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default);

    Task<Stream?> OpenAsync(string storedName, CancellationToken ct = default);
}
