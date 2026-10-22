# Tablo sözlüğü

Bu dosya `02_sema.sql` betiğinden üretilmiştir. Veritabanı Microsoft SQL Server'dadır; şema EF Core Code-First ile modellenmiştir.
Her tabloda `Id` birincil anahtardır (otomatik artan). `CreatedAt` / `UpdatedAt` kayıt zamanını tutar (denetim tablosunda yalnızca `OccurredAt` vardır).

## Genel kurallar

- Hiçbir kayıt silinmez; yabancı anahtarlarda `ON DELETE` kuralı yoktur (yalnızca `AttendanceRecords.WorkPlanDayId` için `SET NULL`).
- Geçmişi olan bilgiler (birim, danışman, program katılımı) tarihli satırlardır; güncel olan, bitiş tarihi boş olan satırdır.
- Aylık ve yıllık veriler (plan, devam, izin) tarih alanlarıyla birbirinden ayrılır; her ay yeni satır açılır, eskisinin üzerine yazılmaz.
- Metin olarak saklanan alanlar (Rol, Durum, Tür) okunabilirlik için sayı değil ad olarak yazılır.

Toplam 23 tablo.

## Tablolar

### Departments

Fakültedeki bölümler.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| Name | nvarchar(150) | Hayır |  |
| Code | nvarchar(20) | Hayır |  |
| IsActive | bit | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_Departments_Code` (Code)

### WorkUnits

Öğrencilerin görevlendirildiği çalışma birimleri (dekanlık, kütüphane, laboratuvar vb.).

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| Name | nvarchar(150) | Hayır |  |
| Description | nvarchar(500) | Evet |  |
| IsActive | bit | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_WorkUnits_Name` (Name)

### Users

Tüm kullanıcı hesapları (öğrenci ve personel). Rol bu tabloda tutulur.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| FullName | nvarchar(200) | Hayır |  |
| Email | nvarchar(254) | Hayır |  |
| PhoneNumber | nvarchar(20) | Evet |  |
| PasswordHash | nvarchar(500) | Hayır |  |
| Role | nvarchar(30) | Hayır |  |
| DepartmentId | int | Evet | FK → Departments |
| IsActive | bit | Hayır |  |
| LastLoginAt | datetime2 | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_Users_DepartmentId` (DepartmentId)
- Benzersiz `IX_Users_Email` (Email)

### StudentProfiles

Öğrencinin kişisel ve akademik bilgileri. Users ile bire bir.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| UserId | int | Hayır | FK → Users |
| NationalId | nchar(11) | Hayır |  |
| Iban | nvarchar(26) | Hayır |  |
| StudentNumber | nvarchar(20) | Hayır |  |
| DepartmentId | int | Hayır | FK → Departments |
| RegistrationYear | int | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_StudentProfiles_DepartmentId` (DepartmentId)
- Benzersiz `IX_StudentProfiles_NationalId` (NationalId)
- Benzersiz `IX_StudentProfiles_StudentNumber` (StudentNumber)
- Benzersiz `IX_StudentProfiles_UserId` (UserId)

### ProgramParticipations

Öğrencinin İŞKUR programına katılım dönemleri. Bitiş tarihi boşsa öğrenci aktiftir; farklı dönemlerde katılım ayrı satırdır.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| StartDate | date | Hayır |  |
| EndDate | date | Evet |  |
| EndReason | nvarchar(1000) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `UX_ProgramParticipations_OneOpenPerStudent` (StudentProfileId) – koşul: [EndDate] IS NULL

### StudentUnitAssignments

Öğrencinin çalıştığı birimin tarihli geçmişi. Açık (bitişi boş) satır güncel birimdir.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| WorkUnitId | int | Hayır | FK → WorkUnits |
| StartDate | date | Hayır |  |
| EndDate | date | Evet |  |
| AssignedByUserId | int | Hayır | FK → Users |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_StudentUnitAssignments_AssignedByUserId` (AssignedByUserId)
- `IX_StudentUnitAssignments_WorkUnitId` (WorkUnitId)
- Benzersiz `UX_StudentUnitAssignments_OneOpenPerStudent` (StudentProfileId) – koşul: [EndDate] IS NULL

### AdvisorAssignments

Öğrencinin danışmanının tarihli geçmişi. Açık satır güncel danışmandır.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| AdvisorUserId | int | Hayır | FK → Users |
| StartDate | date | Hayır |  |
| EndDate | date | Evet |  |
| AssignedByUserId | int | Hayır | FK → Users |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_AdvisorAssignments_AdvisorUserId` (AdvisorUserId)
- `IX_AdvisorAssignments_AssignedByUserId` (AssignedByUserId)
- Benzersiz `UX_AdvisorAssignments_OneOpenPerStudent` (StudentProfileId) – koşul: [EndDate] IS NULL

### StudentCourseDays

Öğrencinin beyan ettiği haftalık ders günleri.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| DayOfWeek | int | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_StudentCourseDays_StudentProfileId_DayOfWeek` (StudentProfileId, DayOfWeek)

### Holidays

Resmî tatil günleri.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| Date | date | Hayır |  |
| Name | nvarchar(150) | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_Holidays_Date` (Date)

### ExamPeriods

Vize ve final dönemleri (bu haftalarda haftalık en az 1 gün şartı aranmaz).

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| Name | nvarchar(150) | Hayır |  |
| StartDate | date | Hayır |  |
| EndDate | date | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

### MonthlyWorkPlans

Öğrencinin her ay için tek aylık planı. (öğrenci, yıl, ay) benzersizdir; böylece farklı ay ve yıllar ayrı kayıt olur.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| Year | int | Hayır |  |
| Month | int | Hayır |  |
| Status | nvarchar(20) | Hayır |  |
| StudentNote | nvarchar(1000) | Evet |  |
| LastSavedAt | datetime2 | Hayır |  |
| ReviewedByUserId | int | Evet | FK → Users |
| ReviewedAt | datetime2 | Evet |  |
| ReviewNote | nvarchar(1000) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_MonthlyWorkPlans_ReviewedByUserId` (ReviewedByUserId)
- Benzersiz `IX_MonthlyWorkPlans_StudentProfileId_Year_Month` (StudentProfileId, Year, Month)

### WorkPlanDays

Bir planda seçilen çalışma günleri. Her gün 8 saattir.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| MonthlyWorkPlanId | int | Hayır | FK → MonthlyWorkPlans |
| Date | date | Hayır |  |
| IsLessonDay | bit | Hayır |  |
| Note | nvarchar(500) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_WorkPlanDays_MonthlyWorkPlanId_Date` (MonthlyWorkPlanId, Date)

### LeaveRequests

İzin talepleri. Süre gün ve saat cinsinden, onay bilgisi (kim, ne zaman) ile birlikte tutulur.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| Type | nvarchar(20) | Hayır |  |
| Status | nvarchar(20) | Hayır |  |
| StartDate | date | Hayır |  |
| EndDate | date | Hayır |  |
| Hours | decimal(5,2) | Evet |  |
| DaysCounted | decimal(5,2) | Hayır |  |
| Reason | nvarchar(300) | Hayır |  |
| Description | nvarchar(1000) | Evet |  |
| AppliedOn | date | Hayır |  |
| ReviewedByUserId | int | Evet | FK → Users |
| ReviewedAt | datetime2 | Evet |  |
| ReviewNote | nvarchar(1000) | Evet |  |
| DocumentFileName | nvarchar(255) | Evet |  |
| DocumentStoredName | nvarchar(100) | Evet |  |
| DocumentContentType | nvarchar(100) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_LeaveRequests_ReviewedByUserId` (ReviewedByUserId)
- `IX_LeaveRequests_StudentProfileId_StartDate` (StudentProfileId, StartDate)

### Absences

Fakülte yönetiminin girdiği devamsızlıklar. Geri alınan kayıt silinmez, geri alma bilgisi yazılır.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| Date | date | Hayır |  |
| Type | nvarchar(20) | Hayır |  |
| Note | nvarchar(500) | Evet |  |
| RecordedByUserId | int | Hayır | FK → Users |
| RevokedAt | datetime2 | Evet |  |
| RevokedByUserId | int | Evet | FK → Users |
| RevokeReason | nvarchar(500) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_Absences_RecordedByUserId` (RecordedByUserId)
- `IX_Absences_RevokedByUserId` (RevokedByUserId)
- Benzersiz `UX_Absences_OneActivePerStudentDay` (StudentProfileId, Date) – koşul: [RevokedAt] IS NULL

### AttendanceSessions

Karekodla devam için açılan günlük oturumlar. SecretKey karekod imzası içindir, dışarıya verilmez.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| Date | date | Hayır |  |
| StartTime | time | Hayır |  |
| EndTime | time | Hayır |  |
| Title | nvarchar(150) | Evet |  |
| DepartmentId | int | Evet | FK → Departments |
| SecretKey | nvarchar(100) | Hayır |  |
| CreatedByUserId | int | Hayır | FK → Users |
| ClosedAt | datetime2 | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_AttendanceSessions_CreatedByUserId` (CreatedByUserId)
- `IX_AttendanceSessions_Date` (Date)
- `IX_AttendanceSessions_DepartmentId` (DepartmentId)

### AttendanceRecords

Öğrencilerin karekodla veya elle oluşan devam kayıtları. Geri alınan kayıt silinmez.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| AttendanceSessionId | int | Hayır | FK → AttendanceSessions |
| Date | date | Hayır |  |
| CheckedInAt | datetime2 | Hayır |  |
| WorkPlanDayId | int | Evet | FK → WorkPlanDays (SET NULL) |
| IsManual | bit | Hayır |  |
| ManualReason | nvarchar(500) | Evet |  |
| CreatedByUserId | int | Evet | FK → Users |
| VoidedAt | datetime2 | Evet |  |
| VoidedByUserId | int | Evet | FK → Users |
| VoidReason | nvarchar(500) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_AttendanceRecords_AttendanceSessionId` (AttendanceSessionId)
- `IX_AttendanceRecords_CreatedByUserId` (CreatedByUserId)
- `IX_AttendanceRecords_StudentProfileId_Date` (StudentProfileId, Date)
- `IX_AttendanceRecords_VoidedByUserId` (VoidedByUserId)
- `IX_AttendanceRecords_WorkPlanDayId` (WorkPlanDayId)
- Benzersiz `UX_Attendance_OneActivePerStudentSession` (StudentProfileId, AttendanceSessionId) – koşul: [VoidedAt] IS NULL

### StudentTasks

Öğrenciye verilen görevler.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| Title | nvarchar(150) | Hayır |  |
| Description | nvarchar(2000) | Evet |  |
| AssignedDate | date | Hayır |  |
| DueDate | date | Hayır |  |
| AssignedByUserId | int | Hayır | FK → Users |
| Status | nvarchar(20) | Hayır |  |
| StatusChangedAt | datetime2 | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_StudentTasks_AssignedByUserId` (AssignedByUserId)
- `IX_StudentTasks_StudentProfileId_Status` (StudentProfileId, Status)

### TaskEvaluations

Görevlere yazılan danışman notları ve durum değişiklikleri; her biri tarihiyle ayrı satırdır.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentTaskId | int | Hayır | FK → StudentTasks |
| AuthorUserId | int | Hayır | FK → Users |
| Note | nvarchar(2000) | Evet |  |
| NewStatus | nvarchar(20) | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_TaskEvaluations_AuthorUserId` (AuthorUserId)
- `IX_TaskEvaluations_StudentTaskId` (StudentTaskId)

### AbsenceNotices

Danışmanın bölüm başkanına gönderdiği devamsızlık bildirimleri.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| Date | date | Hayır |  |
| Message | nvarchar(1000) | Hayır |  |
| AdvisorUserId | int | Hayır | FK → Users |
| CreatedAt | datetime2 | Hayır |  |
| ReadAt | datetime2 | Evet |  |
| ReadByUserId | int | Evet | FK → Users |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_AbsenceNotices_AdvisorUserId_StudentProfileId_Date` (AdvisorUserId, StudentProfileId, Date)
- `IX_AbsenceNotices_ReadByUserId` (ReadByUserId)
- `IX_AbsenceNotices_StudentProfileId` (StudentProfileId)

### TerminationDecisions

İlişik kesme karar kayıtları.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| StudentProfileId | int | Hayır | FK → StudentProfiles |
| Basis | nvarchar(30) | Hayır |  |
| DecisionDate | date | Hayır |  |
| Note | nvarchar(1000) | Hayır |  |
| LeaveDaysUsed | decimal(5,2) | Hayır |  |
| UnexcusedAbsenceDays | int | Hayır |  |
| DecidedByUserId | int | Hayır | FK → Users |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- `IX_TerminationDecisions_DecidedByUserId` (DecidedByUserId)
- `IX_TerminationDecisions_StudentProfileId_DecisionDate` (StudentProfileId, DecisionDate)

### PasswordResetTokens

Parola sıfırlama bağlantıları. Jetonun kendisi değil SHA-256 özeti saklanır.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| UserId | int | Hayır | FK → Users |
| TokenHash | nvarchar(64) | Hayır |  |
| ExpiresAt | datetime2 | Hayır |  |
| UsedAt | datetime2 | Evet |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_PasswordResetTokens_TokenHash` (TokenHash)
- `IX_PasswordResetTokens_UserId` (UserId)

### SystemSettings

Yetkililerin değiştirebildiği ayarlar (izin hakkı, devamsızlık sınırı vb.).

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | int | Hayır | PK |
| Key | nvarchar(100) | Hayır |  |
| Value | nvarchar(500) | Hayır |  |
| CreatedAt | datetime2 | Hayır |  |
| UpdatedAt | datetime2 | Evet |  |

İndeksler:

- Benzersiz `IX_SystemSettings_Key` (Key)

### AuditLogs

Denetim kaydı: kim, ne zaman, hangi kaydı değiştirdi.

| Alan | Tür | Boş olabilir | Anahtar |
|---|---|---|---|
| Id | bigint | Hayır | PK |
| OccurredAt | datetime2 | Hayır |  |
| UserId | int | Evet |  |
| UserEmail | nvarchar(254) | Evet |  |
| Action | nvarchar(100) | Hayır |  |
| EntityName | nvarchar(100) | Hayır |  |
| EntityId | int | Evet |  |
| Details | nvarchar(1000) | Evet |  |
| IpAddress | nvarchar(45) | Evet |  |

İndeksler:

- `IX_AuditLogs_OccurredAt` (OccurredAt)
- `IX_AuditLogs_UserId` (UserId)
