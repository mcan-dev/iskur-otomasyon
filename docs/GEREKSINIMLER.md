# Gereksinim izlenebilirliği

Gereksinim dokümanındaki her bölümün hangi modülde karşılandığı, hangi uç noktalarla kullanıldığı ve hangi testlerle denetlendiği.
Uç nokta ayrıntıları [API.md](API.md), test sınıfları [TEST-SENARYOLARI.md](TEST-SENARYOLARI.md) içindedir.

| Belge bölümü | Karşılığı | Uç noktalar | Testler |
|---|---|---|---|
| 1. Projenin amacı | İlişkisel veritabanı, geçmişin korunması, sorgulama ve rapor | tümü | tümü |
| 2. Roller ve yetkiler | Dört rol; controller'da rol, serviste kapsam denetimi | tüm modüller | yetkisiz erişim testleri (her servis test sınıfında) |
| 3. Öğrenci kayıt ve profil | Kayıt formu, profil, program katılım dönemleri, birim ve danışman geçmişi | `auth/register-student`, `students/*`, `work-units`, `departments` | `AuthServiceTests`, `StudentHistoryTests`, `StudentVisibilityTests`, `DepartmentAndUnitServiceTests`, `UserServiceTests` |
| 4. Aylık çalışma planı | Haftalık 1-3 gün kuralı, perşembe kuralı, tatil/sınav/ders günü uyarıları, onay | `work-plans/*`, `students/me/course-days`, `calendar/*` | `WorkPlanServiceTests`, `WorkPlanRulesTests`, `WorkPlanCalendarTests`, `WorkPlanSettingsTests`, `CourseDayAndCalendarServiceTests` |
| 5. İzin ve devamsızlık | Yıllık izin hakkı, talep/onay, belge, uyarılar, devamsızlık kaydı | `leaves/*`, `absences/*`, `settings/leave` | `LeaveServiceTests`, `LeaveBalanceCalculatorTests`, `LeaveDayCounterTests`, `AbsenceServiceTests` |
| 6. Dinamik karekod | 30 saniyelik imzalı jeton, oturum, çift kayıt engeli, plan/izin kontrolü | `attendance/*` | `AttendanceServiceTests`, `QrTokenTests`, `AttendanceComparerTests` |
| 7. Çalışma saati ve raporlama | Aylık/yıllık rapor, filtre, CSV/Excel, aylık döküm, ilişik kesme | `reports/*`, `terminations` | `ReportServiceTests`, `ReportFileWriterTests`, `WorkFiguresTests`, `TerminationServiceTests` |
| 8. Görev ve değerlendirme | Görev atama, not, durum, devamsızlık bildirimi | `tasks/*`, `absence-notices/*` | `TaskServiceTests`, `AbsenceNoticeServiceTests` |
| 9. Veritabanı tasarımı ve geçmiş | Code-First şema, filtreli benzersiz indeksler, silme yok | - | `DatabaseConstraintTests` ve servis testlerindeki geçmiş senaryoları |
| 10. Sorgulama ve filtreleme | Öğrenci, devam, izin, devamsızlık ve rapor filtreleri | `students`, `attendance`, `leaves`, `absences`, `reports/*` | ilgili servis testleri |
| 11. Güvenlik ve kişisel veri | JWT, rol/kapsam denetimi, TC/IBAN maskeleme, denetim kaydı, hata gizleme | `audit-logs` | `AuthServiceTests`, `PersonalDataMaskerTests`, `StudentVisibilityTests` |
| 12. Beklenen çıktılar | Aşağıdaki tablo | | |

## Beklenen çıktılar (belge bölüm 12)

| # | Çıktı | Durum | Nerede |
|---|---|---|---|
| 1 | Gereksinim analizi | Hazır | Bu dosya, [ARCHITECTURE.md](../ARCHITECTURE.md), [NEDEN-BOYLE.md](NEDEN-BOYLE.md) |
| 2 | Veritabanı tasarımı | Hazır | [DATABASE.md](DATABASE.md) |
| 3 | Kullanıcı arayüzü | Sırada | Arka yüz tamamlandıktan sonra |
| 4 | Çalışma planı modülü | Hazır | Aşama 2 |
| 5 | İzin ve devam modülü | Hazır | Aşama 3 ve 4 |
| 6 | Görev ve değerlendirme modülü | Hazır | Aşama 5 |
| 7 | Raporlama modülü | Hazır | Aşama 5 |
| 8 | Yetkilendirme sistemi | Hazır | Tüm aşamalar |
| 9 | Test senaryoları | Hazır | [TEST-SENARYOLARI.md](TEST-SENARYOLARI.md) |
| 10 | Kullanım ve teknik dokümantasyon | Hazır | [KURULUM.md](KURULUM.md), [KULLANIM.md](KULLANIM.md), [API.md](API.md) |
