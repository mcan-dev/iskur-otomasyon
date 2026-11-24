using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IskurAuto.Data.Context;

/// <summary>
/// Veritabanındaki tablo ve sütun adlarını Türkçeleştirir. Kodda (C# sınıfları ve özellikleri) İngilizce adlar
/// kalır; yalnızca veritabanı tarafındaki adlar değişir. Adlar SQL'de rahat yazılsın diye Türkçe karaktersiz
/// (ASCII) yazılmıştır: Kullanicilar, AdSoyad, SicilNo gibi.
/// </summary>
public static class TurkishNaming
{
    private static readonly Dictionary<string, string> Tables = new()
    {
        ["AttendanceSessions"] = "DevamOturumlari",
        ["AttendanceRecords"] = "DevamKayitlari",
        ["LeaveRequests"] = "Izinler",
        ["Absences"] = "Devamsizliklar",
        ["SystemSettings"] = "SistemAyarlari",
        ["Departments"] = "Bolumler",
        ["WorkUnits"] = "CalismaBirimleri",
        ["Users"] = "Kullanicilar",
        ["AuditLogs"] = "DenetimKayitlari",
        ["Holidays"] = "ResmiTatiller",
        ["ExamPeriods"] = "SinavDonemleri",
        ["StudentCourseDays"] = "OgrenciDersGunleri",
        ["MonthlyWorkPlans"] = "AylikCalismaPlanlari",
        ["WorkPlanDays"] = "PlanGunleri",
        ["StudentTasks"] = "Gorevler",
        ["TaskEvaluations"] = "GorevDegerlendirmeleri",
        ["AbsenceNotices"] = "DevamsizlikBildirimleri",
        ["TerminationDecisions"] = "IlisikKesmeKararlari",
        ["PasswordResetTokens"] = "SifreSifirlamaJetonlari",
        ["StudentProfiles"] = "Ogrenciler",
        ["ProgramParticipations"] = "ProgramKatilimlari",
        ["StudentUnitAssignments"] = "BirimAtamalari",
        ["AdvisorAssignments"] = "SorumluPersonelAtamalari"
    };

    private static readonly Dictionary<string, string> Columns = new()
    {
        ["Id"] = "Id",
        ["CreatedAt"] = "OlusturmaZamani",
        ["UpdatedAt"] = "GuncellemeZamani",
        ["StudentProfileId"] = "OgrenciId",
        ["Date"] = "Tarih",
        ["Type"] = "Tur",
        ["Note"] = "Notu",
        ["RecordedByUserId"] = "KaydedenKullaniciId",
        ["RevokedAt"] = "GeriAlmaZamani",
        ["RevokedByUserId"] = "GeriAlanKullaniciId",
        ["RevokeReason"] = "GeriAlmaGerekcesi",
        ["Message"] = "Mesaj",
        ["AdvisorUserId"] = "SorumluPersonelId",
        ["ReadAt"] = "OkunmaZamani",
        ["ReadByUserId"] = "OkuyanKullaniciId",
        ["StartDate"] = "BaslangicTarihi",
        ["EndDate"] = "BitisTarihi",
        ["AssignedByUserId"] = "AtayanKullaniciId",
        ["AttendanceSessionId"] = "DevamOturumuId",
        ["CheckedInAt"] = "OkutmaZamani",
        ["WorkPlanDayId"] = "PlanGunuId",
        ["IsManual"] = "ElleGirildi",
        ["ManualReason"] = "ElleGirisGerekcesi",
        ["CreatedByUserId"] = "OlusturanKullaniciId",
        ["VoidedAt"] = "IptalZamani",
        ["VoidedByUserId"] = "IptalEdenKullaniciId",
        ["VoidReason"] = "IptalGerekcesi",
        ["StartTime"] = "BaslangicSaati",
        ["EndTime"] = "BitisSaati",
        ["Title"] = "Baslik",
        ["DepartmentId"] = "BolumId",
        ["SecretKey"] = "GizliAnahtar",
        ["ClosedAt"] = "KapanmaZamani",
        ["OccurredAt"] = "IslemZamani",
        ["UserId"] = "KullaniciId",
        ["UserEmail"] = "KullaniciEposta",
        ["Action"] = "Islem",
        ["EntityName"] = "KayitTuru",
        ["EntityId"] = "KayitId",
        ["Details"] = "Ayrinti",
        ["IpAddress"] = "IpAdresi",
        ["Name"] = "Ad",
        ["Code"] = "Kod",
        ["IsActive"] = "AktifMi",
        ["Status"] = "Durum",
        ["DaysCounted"] = "SayilanGun",
        ["Description"] = "Aciklama",
        ["AppliedOn"] = "AlinmaTarihi",
        ["CancelledByUserId"] = "IptalEdenKullaniciId",
        ["CancelledAt"] = "IptalZamani",
        ["CancelNote"] = "IptalNotu",
        ["Year"] = "Yil",
        ["Month"] = "Ay",
        ["StudentNote"] = "OgrenciNotu",
        ["LastSavedAt"] = "SonKayitZamani",
        ["ReviewedByUserId"] = "InceleyenKullaniciId",
        ["ReviewedAt"] = "IncelemeZamani",
        ["ReviewNote"] = "IncelemeNotu",
        ["TokenHash"] = "JetonOzeti",
        ["ExpiresAt"] = "GecerlilikBitisi",
        ["UsedAt"] = "KullanilmaZamani",
        ["EndReason"] = "BitisNedeni",
        ["DayOfWeek"] = "Gun",
        ["Session"] = "DersZamani",
        ["NationalId"] = "TcKimlikNo",
        ["Iban"] = "Iban",
        ["StudentNumber"] = "OgrenciNo",
        ["RegistrationYear"] = "KayitYili",
        ["LeaveDayAllowance"] = "OzelIzinHakki",
        ["AssignedDate"] = "VerilisTarihi",
        ["DueDate"] = "SonTarih",
        ["StatusChangedAt"] = "DurumDegisimZamani",
        ["WorkUnitId"] = "CalismaBirimiId",
        ["Key"] = "Anahtar",
        ["Value"] = "Deger",
        ["StudentTaskId"] = "GorevId",
        ["AuthorUserId"] = "YazanKullaniciId",
        ["NewStatus"] = "YeniDurum",
        ["Basis"] = "Dayanak",
        ["DecisionDate"] = "KararTarihi",
        ["LeaveDaysUsed"] = "KullanilanIzinGunu",
        ["AbsenceDays"] = "DevamsizlikGunu",
        ["DecidedByUserId"] = "KararVerenKullaniciId",
        ["FullName"] = "AdSoyad",
        ["Email"] = "Eposta",
        ["RegistryNumber"] = "SicilNo",
        ["PhoneNumber"] = "Telefon",
        ["PasswordHash"] = "SifreOzeti",
        ["Role"] = "Rol",
        ["LastLoginAt"] = "SonGirisZamani",
        ["MonthlyWorkPlanId"] = "AylikPlanId",
        ["IsLessonDay"] = "DersliGunMu"
    };

    /// <summary>Yapılandırmalar uygulandıktan sonra çağrılır; tablo, sütun ve indeks süzgeçlerini Türkçe adlara çevirir.</summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is not null && Tables.TryGetValue(table, out var turkishTable))
                entity.SetTableName(turkishTable);

            var renamed = new Dictionary<string, string>();
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName();
                if (Columns.TryGetValue(property.Name, out var turkishColumn) && column != turkishColumn)
                {
                    property.SetColumnName(turkishColumn);
                    renamed[column] = turkishColumn;
                }
            }

            // İndeks süzgeçleri ham SQL'dir ([EndDate] IS NULL gibi); sütun adları burada da çevrilir.
            foreach (IMutableIndex index in entity.GetIndexes())
            {
                var filter = index.GetFilter();
                if (string.IsNullOrEmpty(filter)) continue;
                foreach (var (from, to) in renamed)
                    filter = filter.Replace($"[{from}]", $"[{to}]");
                index.SetFilter(filter);
            }
        }
    }
}
