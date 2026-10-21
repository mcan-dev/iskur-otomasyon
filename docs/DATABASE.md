# Veritabanı (SQLite + EF Core Code-First)

## Aşama 1'de var olan tablolar

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

    Department { int Id string Name string Code bool IsActive }
    WorkUnit { int Id string Name string Description bool IsActive }
    User { int Id string FullName string Email string PasswordHash enum Role int DepartmentId bool IsActive }
    StudentProfile { int Id int UserId string NationalId string Iban string StudentNumber int DepartmentId int RegistrationYear }
    ProgramParticipation { int Id date StartDate date EndDate string EndReason }
    StudentUnitAssignment { int Id date StartDate date EndDate }
    AdvisorAssignment { int Id date StartDate date EndDate }
    AuditLog { long Id datetime OccurredAt string Action string EntityName int EntityId }
```

## Benzersizlik ve bütünlük kuralları (veritabanı seviyesinde)

| Kural | Mekanizma |
|---|---|
| E-posta, TC, öğrenci no, bölüm kodu/adı, birim adı benzersiz | Unique index |
| Öğrencinin aynı anda tek açık katılımı / birimi / danışmanı | Filtreli unique index `WHERE "EndDate" IS NULL` |
| Geçmişi olan kayıt silinemez | Tüm FK'lar `Restrict` |
| Rol okunabilir | `UserRole` metin olarak saklanır |

## Sonraki aşamalarda eklenecek tablolar (planlanan)

`WorkPlan` / `WorkPlanDay` (aylık plan, gün durumu), `Holiday` ve `CourseSchedule` (kurallar), `LeaveRequest` ve `Absence`
(izin/devamsızlık, limit `SystemSetting` tablosunda), `QrSession` ve `Attendance` (30 sn QR + devam), `TaskAssignment` ve
`AdvisorEvaluation` (görev/değerlendirme). Her biri aşama sırasında bu belgeye eklenecektir.

## Migration

Bu depoda migration dosyası **yoktur** (eski model değiştiği için silindi). İlk kurulumda üretilir:

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/IskurAuto.Data --startup-project src/IskurAuto.API
```

Uygulama açılırken migration varsa `Migrate()`, yoksa `EnsureCreated()` çalışır.
