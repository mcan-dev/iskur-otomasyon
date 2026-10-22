using IskurAuto.Core.Entities;
using IskurAuto.Core.Models;

namespace IskurAuto.Core.Interfaces;

public interface IStudentRepository : IRepository<StudentProfile>
{
    /// <summary>
    /// Öğrenciyi tüm ilişkileriyle (kullanıcı, bölüm, katılım dönemleri, birim ve danışman geçmişi)
    /// getirir. Güncelleme için kullanılabilsin diye izlenen (tracked) halde döner.
    /// </summary>
    Task<StudentProfile?> GetDetailAsync(int studentId, CancellationToken ct = default);

    Task<StudentProfile?> GetDetailByUserIdAsync(int userId, CancellationToken ct = default);

    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct = default);

    Task<bool> StudentNumberExistsAsync(string studentNumber, CancellationToken ct = default);

    /// <summary>Filtreli ve sayfalı öğrenci listesi (salt okunur; izlenmez).</summary>
    Task<PagedResult<StudentProfile>> SearchAsync(
        StudentSearchCriteria criteria, PageRequest page, CancellationToken ct = default);

    /// <summary>Sayfalamadan, rapor için tüm eşleşen öğrenciler (salt okunur).</summary>
    Task<IReadOnlyList<StudentProfile>> ListAsync(StudentSearchCriteria criteria, CancellationToken ct = default);
}
