# Test senaryoları

Belgenin 9. maddesindeki test başlıkları ve bunları karşılayan otomatik testler. Toplam 275 test yöntemi vardır; `[Theory]` testlerinin her veri satırı ayrı çalıştığı için `dotnet test` çıktısındaki sayı daha yüksektir.

```bash
dotnet test                                         # hepsi
dotnet test --filter "FullyQualifiedName~Attendance" # bir modül
```

Veritabanını kullanan testler her test için ayrı, bellek içi bir SQLite veritabanı kurar (testlerin hızlı ve kurulumsuz çalışması için; model ve sorgular sağlayıcıdan bağımsızdır, uygulama SQL Server kullanır). Saat sabittir (21 Ekim 2026, 12:00), bu yüzden sonuçlar günden bağımsızdır.

## Belgedeki başlıklara göre

### Hatalı veri girişi
T.C. kimlik no ve IBAN algoritması, telefon ve ad normalleştirme, parola kuralları, gelecekteki tarih, kısa gerekçe, mükerrer kayıt, tek seferde tüm alan hatalarının dönmesi, hata olunca yarım kayıt kalmaması.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `NationalIdValidatorTests` | 3 | `Unit/ValidationTests.cs` |
| `IbanValidatorTests` | 3 | `Unit/ValidationTests.cs` |
| `PasswordPolicyTests` | 2 | `Unit/ValidationTests.cs` |
| `InputNormalizerTests` | 4 | `Unit/ValidationTests.cs` |
| `AuthServiceTests` | 16 | `Services/AuthServiceTests.cs` |
| `PasswordResetServiceTests` | 7 | `Services/PasswordResetServiceTests.cs` |
| `DatabaseConstraintTests` | 8 | `Services/AdminAndDatabaseTests.cs` |

Her modülün servis testlerinde de geçersiz girdiye karşı senaryolar vardır (örneğin `Assign_WithPastDueDate_IsRejected`, `Record_NeedsARealReason_AndASensibleDate`).

### Yetkisiz erişim
Rol kontrolü ve kapsam: bölüm başkanı yalnızca kendi bölümünü, danışman yalnızca kendi öğrencilerini, öğrenci yalnızca kendi kaydını görür; kapsam dışı kayıt 404 döner; filtreyle kapsam genişletilemez; TC/IBAN yalnızca fakülte yönetimi ve öğrenciye açıktır.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `StudentVisibilityTests` | 12 | `Services/StudentServiceTests.cs` |
| `UserServiceTests` | 7 | `Services/AdminAndDatabaseTests.cs` |
| `DepartmentAndUnitServiceTests` | 4 | `Services/AdminAndDatabaseTests.cs` |

Rol ve kapsam senaryoları diğer sınıflarda da modül bazında vardır: `*_IsForbidden`, `*_FollowTheRoleScope`, `*_OutsideTheScope_LooksLikeNotFound`, `OnlyStudentsCanCheckIn`, `OnlyTheFacultyOfficeRecordsDecisions`, `MonthlyStatement_IsOnlyForTheFacultyOffice`.

### Çalışma günü sınırları
Haftada 1-3 gün, haftanın perşembesinin düştüğü aya sayılması, hafta sonu/tatil/program dışı günün seçilememesi, dersli günde açıklama zorunluluğu, sınav haftasında alt sınırın aranmaması, her günün 8 saat sayılması, düzeltme süresi, onaylı planın kilitlenmesi.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `WorkPlanRulesTests` | 14 | `Unit/WorkPlanRulesTests.cs` |
| `WorkPlanSettingsTests` | 1 | `Unit/WorkPlanRulesTests.cs` |
| `WorkPlanCalendarTests` | 4 | `Unit/WorkPlanRulesTests.cs` |
| `WorkPlanServiceTests` | 17 | `Services/WorkPlanServiceTests.cs` |
| `CourseDayAndCalendarServiceTests` | 5 | `Services/WorkPlanServiceTests.cs` |

### İzin hesaplamaları
Plan varsa planlı günlerin, yoksa hafta içi günlerin sayılması, tatillerin düşülmesi, saatlik izin, yıl sınırı, çakışan talep, sınır uyarıları (yaklaşma, aşma, bekleyen talepler), izin ve devamsızlığın tek haktan düşmesi, öğrenciye özel izin hakkı, geri alınan kaydın bakiyeye etkisi.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `LeaveDayCounterTests` | 6 | `Unit/LeaveCalculationTests.cs` |
| `LeaveBalanceCalculatorTests` | 13 | `Unit/LeaveCalculationTests.cs` |
| `LeaveServiceTests` | 19 | `Services/LeaveServiceTests.cs` |
| `AbsenceServiceTests` | 6 | `Services/LeaveServiceTests.cs` |

### Karekod geçerliliği
Jetonun 30 saniyede değişmesi, tolerans penceresinin dışında geçersizleşmesi, başka oturumda ya da değiştirilmiş imzayla geçersiz olması, aynı oturumda ikinci kayıt, oturum saati dışında okutma, planı olmayan ya da izinli öğrencinin reddedilmesi, reddedilen denemenin denetime yazılması, gerekçeli iptal ve elle kayıt, planlanan-gerçekleşen karşılaştırması.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `QrTokenTests` | 10 | `Unit/QrTokenTests.cs` |
| `AttendanceServiceTests` | 32 | `Services/AttendanceServiceTests.cs` |
| `AttendanceComparerTests` | 6 | `Unit/AttendanceComparerTests.cs` |

### Raporların doğruluğu
Aylık/yıllık saat hesabı, plan dışı gün ve onaysız planın saate eklenmemesi, filtreler, rol kapsamı, sınıra yaklaşan ve ilişik kesme değerlendirmesi gerektiren listeler, aylık dökümün içeriği ve erişimi, CSV ve Excel dosyalarının biçimi, dışa aktarmanın denetlenmesi, ilişik kesme kararının öğrenciyi pasifleştirmesi ve geçmişi koruması.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `WorkFiguresTests` | 4 | `Unit/WorkFiguresTests.cs` |
| `ReportServiceTests` | 16 | `Services/ReportServiceTests.cs` |
| `ReportFileWriterTests` | 11 | `Unit/ReportFileWriterTests.cs` |
| `TerminationServiceTests` | 6 | `Services/TerminationServiceTests.cs` |

### Görev ve değerlendirme
Değerlendirme geçmişinin eklenerek korunması, yalnızca şu anki danışmanın değerlendirebilmesi, danışman değişince eski notların kalması, bölüm başkanı kapsamı, devamsızlık bildirimi.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `TaskServiceTests` | 16 | `Services/TaskServiceTests.cs` |
| `AbsenceNoticeServiceTests` | 6 | `Services/AbsenceNoticeServiceTests.cs` |

### Geçmişin korunması ve veritabanı bütünlüğü
Danışman, birim ve katılım geçmişi, silmenin engellenmesi, benzersizlik ve filtreli benzersiz indeksler.

| Test sınıfı | Test sayısı | Dosya |
|---|---|---|
| `StudentHistoryTests` | 9 | `Services/StudentServiceTests.cs` |
| `DatabaseConstraintTests` | 8 | `Services/AdminAndDatabaseTests.cs` |
| `Pbkdf2PasswordHasherTests` | 6 | `Unit/ValidationTests.cs` |
| `PersonalDataMaskerTests` | 2 | `Unit/ValidationTests.cs` |

## Elle kabul testi

Otomatik testler servis katmanını sınar. Arayüz bağlandıktan sonra aşağıdaki akışlar gerçek kullanıcı gözüyle elle denenmelidir (Development ortamında demo hesaplarla, [Kurulum](KURULUM.md)).

| # | Senaryo | Beklenen |
|---|---|---|
| 1 | Yeni öğrenci kayıt formunu geçersiz TC ve IBAN ile gönder | Tüm hatalar alanlarıyla birlikte tek seferde gösterilir |
| 2 | Öğrenci olarak giriş yap, bir ay için 4 gün seç, 1. haftada 0 gün bırak | Kayıt reddedilir, hangi haftanın eksik olduğu yazılır |
| 3 | Aynı ayda hafta sonu ve resmî tatil günü seçmeyi dene | Gün seçilemez, takvimde kırmızı ve etiketle gösterilir |
| 4 | Plan kaydet, fakülte hesabıyla onayla, öğrenciyle tekrar düzenlemeyi dene | Öğrenci düzenleyemez; fakülte düzeltince plan "bekliyor"a döner |
| 5 | 10 günden fazla izin iste | Talep alınır, uyarı gösterilir; fakülte ekranında belirgin işaretlenir |
| 6 | İzin talebine 5 MB'tan büyük belge ya da .exe yükle | Reddedilir |
| 7 | Fakülte hesabıyla karekod oturumu aç, ekranı izle | Karekod yaklaşık her 30 saniyede değişir |
| 8 | Öğrenciyle karekodu okut, sonra aynı karekodu 1 dakika sonra tekrar okut | İlk okutma kaydedilir; ikincisi reddedilir |
| 9 | Planı olmayan öğrenciyle okutmayı dene | Reddedilir, nedeni yazılır |
| 10 | Bölüm başkanıyla başka bölümün öğrencisinin adresini (id) doğrudan aç | Kayıt bulunamadı döner |
| 11 | Danışmanla görev ata, not yaz, durumu "Tamamlanmadı"dan "Tamamlandı"ya çek | Geçmişte üç kayıt da görünür |
| 12 | Danışmanla öğrencinin devamsızlığını bildir, bölüm başkanıyla oku | Bildirim görünür, okundu işaretlenir |
| 13 | Fakülte hesabıyla aylık raporu Excel ve CSV olarak al | Ekrandaki ve dosyadaki satırlar aynıdır; Excel/CSV Türkçe karakterleri doğru açar |
| 14 | Aylık dökümü bölüm başkanıyla açmayı dene | Yetki yok |
| 15 | İlişik kesme listesindeki bir öğrenci için karar kaydet | Öğrenci pasif olur, geçmişi durur, karar listede görünür |
| 16 | Yedekle, veritabanını sil, yedekten geri yükle | Tüm kayıtlar geri gelir ([Kurulum](KURULUM.md#yedekleme-ve-geri-yükleme)) |

## Bilinen test boşlukları

- Controller, JWT doğrulama ve CORS katmanı için HTTP düzeyinde (entegrasyon) test yoktur; yetki kuralları servis katmanında da uygulandığı için orada sınanır.
- Eşzamanlı isteklerde (aynı anda iki okutma gibi) yarış durumu testi yoktur; tekil kayıt kuralı veritabanındaki benzersiz indeksle de korunur.
- Yük testi yapılmamıştır.
