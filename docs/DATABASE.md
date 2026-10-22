# Veritabanı (SQL Server + EF Core Code-First)

## Tablolar

```mermaid
erDiagram
    Department ||--o{ User : "personel (bölüm başkanı/danışman)"
    Department ||--o{ StudentProfile : "öğrenciler"
    User ||--o| StudentProfile : "öğrenci hesabı (1-1)"
    StudentProfile ||--o{ ProgramParticipation : "katılım dönemleri"
    StudentProfile ||--o{ StudentUnitAssignment : "birim geçmişi"
    StudentProfile ||--o{ AdvisorAssignment : "danışman geçmişi"
    WorkUnit ||--o{ StudentUnitAssignment : ""
    User ||--o{ AdvisorAssignment : "danışman"
    User ||--o{ AuditLog : "işlemi yapan"
    StudentProfile ||--o{ StudentCourseDay : "dersli günler"
    StudentProfile ||--o{ MonthlyWorkPlan : "aylık planlar"
    MonthlyWorkPlan ||--o{ WorkPlanDay : "seçilen günler"
    StudentProfile ||--o{ LeaveRequest : "izin talepleri"
    StudentProfile ||--o{ Absence : "devamsızlıklar"
    User ||--o{ LeaveRequest : "onaylayan"
    User ||--o{ Absence : "kaydeden / geri alan"
    User ||--o{ MonthlyWorkPlan : "onaylayan"
    StudentProfile ||--o{ AttendanceRecord : "devam kayıtları"
    AttendanceSession ||--o{ AttendanceRecord : "oturum"
    Department ||--o{ AttendanceSession : "bölüm oturumu (boşsa fakülte geneli)"
    User ||--o{ AttendanceSession : "açan"
    StudentProfile ||--o{ StudentTask : "görevler"
    StudentTask ||--o{ TaskEvaluation : "değerlendirmeler"
    User ||--o{ StudentTask : "atayan"
    User ||--o{ TaskEvaluation : "yazan danışman"
    StudentProfile ||--o{ AbsenceNotice : "devamsızlık bildirimleri"
    User ||--o{ AbsenceNotice : "bildiren danışman"
    StudentProfile ||--o{ TerminationDecision : "ilişik kesme kararı"
    User ||--o{ TerminationDecision : "kararı veren"
    User ||--o{ PasswordResetToken : "parola sıfırlama bağlantıları"

    Department { int Id string Name string Code bool IsActive }
    WorkUnit { int Id string Name string Description bool IsActive }
    User { int Id string FullName string Email string PasswordHash enum Role int DepartmentId bool IsActive }
    StudentProfile { int Id int UserId string NationalId string Iban string StudentNumber int DepartmentId int RegistrationYear }
    ProgramParticipation { int Id date StartDate date EndDate string EndReason }
    StudentUnitAssignment { int Id date StartDate date EndDate }
    AdvisorAssignment { int Id date StartDate date EndDate }
    Holiday { int Id date Date string Name }
    ExamPeriod { int Id string Name date StartDate date EndDate }
    StudentCourseDay { int Id int StudentProfileId int DayOfWeek }
    MonthlyWorkPlan { int Id int StudentProfileId int Year int Month enum Status string StudentNote datetime LastSavedAt int ReviewedByUserId datetime ReviewedAt string ReviewNote }
    WorkPlanDay { int Id int MonthlyWorkPlanId date Date bool IsLessonDay string Note }
    LeaveRequest { int Id int StudentProfileId enum Type enum Status date StartDate date EndDate decimal Hours decimal DaysCounted string Reason date AppliedOn int ReviewedByUserId datetime ReviewedAt string ReviewNote string DocumentStoredName }
    Absence { int Id int StudentProfileId date Date enum Type string Note int RecordedByUserId datetime RevokedAt string RevokeReason }
    AttendanceSession { int Id date Date time StartTime time EndTime string Title int DepartmentId string SecretKey int CreatedByUserId datetime ClosedAt }
    AttendanceRecord { int Id int StudentProfileId int AttendanceSessionId date Date datetime CheckedInAt int WorkPlanDayId bool IsManual string ManualReason int CreatedByUserId datetime VoidedAt int VoidedByUserId string VoidReason }
    StudentTask { int Id int StudentProfileId string Title string Description date AssignedDate date DueDate int AssignedByUserId enum Status datetime StatusChangedAt }
    TaskEvaluation { int Id int StudentTaskId int AuthorUserId string Note enum NewStatus datetime CreatedAt }
    AbsenceNotice { int Id int StudentProfileId date Date string Message int AdvisorUserId datetime CreatedAt datetime ReadAt int ReadByUserId }
    TerminationDecision { int Id int StudentProfileId enum Basis date DecisionDate string Note decimal LeaveDaysUsed int UnexcusedAbsenceDays int DecidedByUserId datetime CreatedAt }
    PasswordResetToken { int Id int UserId string TokenHash datetime ExpiresAt datetime UsedAt }
    SystemSetting { int Id string Key string Value }
    AuditLog { long Id datetime OccurredAt string Action string EntityName int EntityId }
```

## Benzersizlik ve bütünlük kuralları (veritabanı seviyesinde)

| Kural | Mekanizma |
|---|---|
| E-posta, TC, öğrenci no, bölüm kodu/adı, birim adı benzersiz | Unique index |
| Öğrencinin aynı anda tek açık katılımı / birimi / danışmanı | Filtreli unique index `WHERE [EndDate] IS NULL` |
| Geçmişi olan kayıt silinemez | Tüm FK'lar `Restrict` |
| Rol okunabilir | `UserRole` metin olarak saklanır |
| Öğrencinin bir ay için tek planı | Unique index (öğrenci, yıl, ay) |
| Bir planda aynı gün tek kez | Unique index (plan, tarih) |
| Aynı tarih iki kez tatil olamaz | Unique index (tarih) |
| Öğrencinin bir günde tek aktif devamsızlığı | Filtreli unique index `WHERE [RevokedAt] IS NULL` |
| Aynı öğrencinin aynı oturumda tek geçerli devam kaydı | Filtreli unique index `WHERE [VoidedAt] IS NULL` |
| Aynı danışman aynı öğrenci ve gün için tek devamsızlık bildirimi | Unique index (danışman, öğrenci, tarih) |
| Değerlendirmesi olan görev silinemez | FK `Restrict` |
| İlişik kesme kararı silinemez, öğrenci kaydına bağlıdır | FK `Restrict` |
| Sıfırlama jetonu tektir; jetonun kendisi değil özeti saklanır | Unique index (TokenHash) |
| Ayar anahtarı tektir | Unique index (Key) |

## Raporlar

Raporlar için ayrı tablo yoktur; hepsi yukarıdaki kayıtlardan istek anında hesaplanır.

## SQL Server notları

- Varsayılan bağlantı: `Server=(localdb)\MSSQLLocalDB;Database=IskurAuto;Trusted_Connection=True;TrustServerCertificate=True` (Visual Studio ile gelen LocalDB). Kurumsal sunucuda `ConnectionStrings:IskurAutoDb` ile değiştirilir.
- Hiçbir ilişkide cascade delete yoktur; kayıtlar silinmez, tarihçe tutulur.
- "Açık dönem tek olsun" (`EndDate IS NULL`), "iptal edilmemiş kayıt tek olsun" (`VoidedAt IS NULL`, `RevokedAt IS NULL`) kuralları filtreli benzersiz indekslerle veritabanında da güvence altındadır.
- Ondalık alanlar (`Hours`, `DaysCounted`, `LeaveDaysUsed`) `decimal(5,2)` tutulur.
- Tablo alanlarının tam listesi `teslim/3_Veritabani/04_tablo_sozlugu.md` içindedir.

## Migration

Depoda hazır migration yoktur. Uygulama ilk açılışta veritabanını ve tabloları kendisi oluşturur (`EnsureCreated`).
Migration ile ilerlemek isteyen geliştirici ilk migration'ı üretir ve dosyaları depoya ekler:

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/IskurAuto.Data --startup-project src/IskurAuto.API
```

Bundan sonra model değiştikçe `dotnet ef migrations add <Ad>` ile yeni migration eklenir. Uygulama açılırken
migration varsa `Migrate()` çalışır. Önemli: veritabanı daha önce `EnsureCreated` ile oluştuysa, ilk migration'ı
uygulamadan önce veritabanı silinmelidir (`dotnet ef database drop --project src/IskurAuto.Data --startup-project src/IskurAuto.API`).
