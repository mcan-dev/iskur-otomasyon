namespace IskurAuto.Core.Enums;

/// <summary>
/// Belgenin 2. bölümündeki dört temel kullanıcı rolü.
/// Her kullanıcı yalnızca kendi görev ve yetkileri kapsamında işlem yapabilir.
/// </summary>
public enum UserRole
{
    /// <summary>Dekanlık ve Fakülte Sekreterliği: fakülte genelinde tüm kayıtlar (belge 2.1).</summary>
    FacultyAdmin = 1,

    /// <summary>Bölüm Başkanı: yalnızca kendi bölümündeki öğrenciler (belge 2.2).</summary>
    DepartmentHead = 2,

    /// <summary>Danışman Öğretim Elemanı: yalnızca kendisine atanmış öğrenciler (belge 2.3).</summary>
    Advisor = 3,

    /// <summary>Öğrenci: yalnızca kendi kişisel verileri (belge 2.4).</summary>
    Student = 4
}
