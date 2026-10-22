/*
  Örnek test verisi - İŞKUR Otomasyonu (Selçuk Üniversitesi Teknoloji Fakültesi)

  02_sema.sql çalıştırıldıktan sonra, boş veritabanına bir kez çalıştırılır.
  Tüm kullanıcıların parolası: Demo1234!   (yalnızca deneme içindir, gerçek ortamda kullanılmaz)
  TC kimlik ve IBAN değerleri uydurmadır.

  Veri, aynı öğrencinin farklı ay ve yıllardaki kayıtlarını birbirinden ayırt etmeyi gösterecek biçimdedir:
  Mehmet Öztürk için Eylül 2025, Ekim 2025, Eylül 2026 ve Ekim 2026 planları ayrı kayıtlardır;
  birim ve danışman geçmişi de tarihli satırlar olarak durur.
  Bugün 22 Ekim 2026 kabul edilmiştir.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] ([Id], [Name], [Code], [IsActive], [CreatedAt]) VALUES (1, N'Bilgisayar Mühendisliği', N'BM', 1, SYSUTCDATETIME());
INSERT INTO [Departments] ([Id], [Name], [Code], [IsActive], [CreatedAt]) VALUES (2, N'Yazılım Mühendisliği', N'YM', 1, SYSUTCDATETIME());
INSERT INTO [Departments] ([Id], [Name], [Code], [IsActive], [CreatedAt]) VALUES (3, N'Elektrik-Elektronik Mühendisliği', N'EEM', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [Departments] OFF;

SET IDENTITY_INSERT [WorkUnits] ON;
INSERT INTO [WorkUnits] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (1, N'Dekanlık', N'Dekanlık ofisi', 1, SYSUTCDATETIME());
INSERT INTO [WorkUnits] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (2, N'Fakülte Sekreterliği', NULL, 1, SYSUTCDATETIME());
INSERT INTO [WorkUnits] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (3, N'Bilgisayar Laboratuvarı', N'Laboratuvar destek işleri', 1, SYSUTCDATETIME());
INSERT INTO [WorkUnits] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (4, N'Kütüphane', NULL, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [WorkUnits] OFF;

SET IDENTITY_INSERT [Holidays] ON;
INSERT INTO [Holidays] ([Id], [Date], [Name], [CreatedAt]) VALUES (1, CAST('2025-10-29' AS date), N'Cumhuriyet Bayramı', SYSUTCDATETIME());
INSERT INTO [Holidays] ([Id], [Date], [Name], [CreatedAt]) VALUES (2, CAST('2026-01-01' AS date), N'Yılbaşı', SYSUTCDATETIME());
INSERT INTO [Holidays] ([Id], [Date], [Name], [CreatedAt]) VALUES (3, CAST('2026-04-23' AS date), N'Ulusal Egemenlik ve Çocuk Bayramı', SYSUTCDATETIME());
INSERT INTO [Holidays] ([Id], [Date], [Name], [CreatedAt]) VALUES (4, CAST('2026-05-01' AS date), N'Emek ve Dayanışma Günü', SYSUTCDATETIME());
INSERT INTO [Holidays] ([Id], [Date], [Name], [CreatedAt]) VALUES (5, CAST('2026-05-19' AS date), N'Atatürk''ü Anma, Gençlik ve Spor Bayramı', SYSUTCDATETIME());
INSERT INTO [Holidays] ([Id], [Date], [Name], [CreatedAt]) VALUES (6, CAST('2026-10-29' AS date), N'Cumhuriyet Bayramı', SYSUTCDATETIME());
SET IDENTITY_INSERT [Holidays] OFF;

SET IDENTITY_INSERT [ExamPeriods] ON;
INSERT INTO [ExamPeriods] ([Id], [Name], [StartDate], [EndDate], [CreatedAt]) VALUES (1, N'2025-2026 Güz dönemi final sınavları', CAST('2026-01-12' AS date), CAST('2026-01-25' AS date), SYSUTCDATETIME());
INSERT INTO [ExamPeriods] ([Id], [Name], [StartDate], [EndDate], [CreatedAt]) VALUES (2, N'2026-2027 Güz dönemi ara sınavları', CAST('2026-11-02' AS date), CAST('2026-11-13' AS date), SYSUTCDATETIME());
SET IDENTITY_INSERT [ExamPeriods] OFF;

SET IDENTITY_INSERT [SystemSettings] ON;
INSERT INTO [SystemSettings] ([Id], [Key], [Value], [CreatedAt]) VALUES (1, N'Leave.DayLimit', N'10', SYSUTCDATETIME());
INSERT INTO [SystemSettings] ([Id], [Key], [Value], [CreatedAt]) VALUES (2, N'Leave.WarningRemainingDays', N'2', SYSUTCDATETIME());
INSERT INTO [SystemSettings] ([Id], [Key], [Value], [CreatedAt]) VALUES (3, N'Leave.AbsenceDayLimit', N'5', SYSUTCDATETIME());
INSERT INTO [SystemSettings] ([Id], [Key], [Value], [CreatedAt]) VALUES (4, N'Leave.AbsenceWarningRemainingDays', N'1', SYSUTCDATETIME());
INSERT INTO [SystemSettings] ([Id], [Key], [Value], [CreatedAt]) VALUES (5, N'Leave.HealthCountsTowardLimit', N'false', SYSUTCDATETIME());
SET IDENTITY_INSERT [SystemSettings] OFF;

SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (1, N'Ayşe Kaya', N'sekreter@teknoloji.edu.tr', NULL, N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'FacultyAdmin', NULL, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (2, N'Prof. Dr. Mehmet Demir', N'baskan.bm@teknoloji.edu.tr', NULL, N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'DepartmentHead', 1, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (3, N'Dr. Öğr. Üyesi Zeynep Arslan', N'danisman.bm@teknoloji.edu.tr', NULL, N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Advisor', 1, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (4, N'Dr. Öğr. Üyesi Can Aksoy', N'danisman.ym@teknoloji.edu.tr', NULL, N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Advisor', 2, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (5, N'Mehmet Öztürk', N'ogrenci@teknoloji.edu.tr', N'05321234567', N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Student', 1, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (6, N'Elif Yıldız', N'ogrenci2@teknoloji.edu.tr', N'05329876543', N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Student', 2, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (7, N'Burak Şahin', N'burak.sahin@teknoloji.edu.tr', N'05331112233', N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Student', 1, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (8, N'Selin Çelik', N'selin.celik@teknoloji.edu.tr', N'05344445566', N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Student', 3, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (9, N'Dr. Öğr. Üyesi Hakan Polat', N'danisman2.bm@teknoloji.edu.tr', NULL, N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Advisor', 1, 1, SYSUTCDATETIME());
INSERT INTO [Users] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [DepartmentId], [IsActive], [CreatedAt]) VALUES (10, N'Kerem Aydın', N'kerem.aydin@teknoloji.edu.tr', N'05355556677', N'v1.600000.gcPMyfm1G/3RHPUgZD/L8A==.ce+y6hktSXZmVF9BeHT8yUekCT/NbBD8WUMY+ozbHHg=', N'Student', 1, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [Users] OFF;

SET IDENTITY_INSERT [StudentProfiles] ON;
INSERT INTO [StudentProfiles] ([Id], [UserId], [NationalId], [Iban], [StudentNumber], [DepartmentId], [RegistrationYear], [CreatedAt]) VALUES (1, 5, N'12345678950', N'TR400006200000000000000001', N'2023000001', 1, 2023, SYSUTCDATETIME());
INSERT INTO [StudentProfiles] ([Id], [UserId], [NationalId], [Iban], [StudentNumber], [DepartmentId], [RegistrationYear], [CreatedAt]) VALUES (2, 6, N'23456789138', N'TR130006200000000000000002', N'2024000002', 2, 2024, SYSUTCDATETIME());
INSERT INTO [StudentProfiles] ([Id], [UserId], [NationalId], [Iban], [StudentNumber], [DepartmentId], [RegistrationYear], [CreatedAt]) VALUES (3, 7, N'10000000146', N'TR560006200000000000000003', N'2025000003', 1, 2025, SYSUTCDATETIME());
INSERT INTO [StudentProfiles] ([Id], [UserId], [NationalId], [Iban], [StudentNumber], [DepartmentId], [RegistrationYear], [CreatedAt]) VALUES (4, 8, N'11111111110', N'TR780006200000000000000004', N'2026000004', 3, 2026, SYSUTCDATETIME());
INSERT INTO [StudentProfiles] ([Id], [UserId], [NationalId], [Iban], [StudentNumber], [DepartmentId], [RegistrationYear], [CreatedAt]) VALUES (5, 10, N'10000000278', N'TR900006200000000000000005', N'2023000005', 1, 2023, SYSUTCDATETIME());
SET IDENTITY_INSERT [StudentProfiles] OFF;

SET IDENTITY_INSERT [ProgramParticipations] ON;
INSERT INTO [ProgramParticipations] ([Id], [StudentProfileId], [StartDate], [EndDate], [EndReason], [CreatedAt]) VALUES (1, 1, CAST('2025-09-01' AS date), NULL, NULL, SYSUTCDATETIME());
INSERT INTO [ProgramParticipations] ([Id], [StudentProfileId], [StartDate], [EndDate], [EndReason], [CreatedAt]) VALUES (2, 2, CAST('2025-10-01' AS date), NULL, NULL, SYSUTCDATETIME());
INSERT INTO [ProgramParticipations] ([Id], [StudentProfileId], [StartDate], [EndDate], [EndReason], [CreatedAt]) VALUES (3, 3, CAST('2026-02-01' AS date), NULL, NULL, SYSUTCDATETIME());
INSERT INTO [ProgramParticipations] ([Id], [StudentProfileId], [StartDate], [EndDate], [EndReason], [CreatedAt]) VALUES (4, 4, CAST('2026-09-15' AS date), NULL, NULL, SYSUTCDATETIME());
INSERT INTO [ProgramParticipations] ([Id], [StudentProfileId], [StartDate], [EndDate], [EndReason], [CreatedAt]) VALUES (5, 5, CAST('2025-09-01' AS date), CAST('2026-06-30' AS date), N'İlişik kesme: devamsızlık sınırının aşılması', SYSUTCDATETIME());
SET IDENTITY_INSERT [ProgramParticipations] OFF;

SET IDENTITY_INSERT [StudentUnitAssignments] ON;
INSERT INTO [StudentUnitAssignments] ([Id], [StudentProfileId], [WorkUnitId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (1, 1, 4, CAST('2025-09-01' AS date), CAST('2026-02-28' AS date), 1, SYSUTCDATETIME());
INSERT INTO [StudentUnitAssignments] ([Id], [StudentProfileId], [WorkUnitId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (2, 1, 3, CAST('2026-03-01' AS date), NULL, 1, SYSUTCDATETIME());
INSERT INTO [StudentUnitAssignments] ([Id], [StudentProfileId], [WorkUnitId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (3, 2, 4, CAST('2025-10-01' AS date), NULL, 1, SYSUTCDATETIME());
INSERT INTO [StudentUnitAssignments] ([Id], [StudentProfileId], [WorkUnitId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (4, 3, 1, CAST('2026-02-01' AS date), NULL, 1, SYSUTCDATETIME());
INSERT INTO [StudentUnitAssignments] ([Id], [StudentProfileId], [WorkUnitId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (5, 5, 2, CAST('2025-09-01' AS date), CAST('2026-06-30' AS date), 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [StudentUnitAssignments] OFF;

SET IDENTITY_INSERT [AdvisorAssignments] ON;
INSERT INTO [AdvisorAssignments] ([Id], [StudentProfileId], [AdvisorUserId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (1, 1, 3, CAST('2025-09-01' AS date), CAST('2026-02-28' AS date), 2, SYSUTCDATETIME());
INSERT INTO [AdvisorAssignments] ([Id], [StudentProfileId], [AdvisorUserId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (2, 1, 9, CAST('2026-03-01' AS date), NULL, 2, SYSUTCDATETIME());
INSERT INTO [AdvisorAssignments] ([Id], [StudentProfileId], [AdvisorUserId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (3, 2, 4, CAST('2025-10-01' AS date), NULL, 1, SYSUTCDATETIME());
INSERT INTO [AdvisorAssignments] ([Id], [StudentProfileId], [AdvisorUserId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (4, 3, 3, CAST('2026-02-01' AS date), NULL, 2, SYSUTCDATETIME());
INSERT INTO [AdvisorAssignments] ([Id], [StudentProfileId], [AdvisorUserId], [StartDate], [EndDate], [AssignedByUserId], [CreatedAt]) VALUES (5, 5, 3, CAST('2025-09-01' AS date), CAST('2026-06-30' AS date), 2, SYSUTCDATETIME());
SET IDENTITY_INSERT [AdvisorAssignments] OFF;

SET IDENTITY_INSERT [StudentCourseDays] ON;
INSERT INTO [StudentCourseDays] ([Id], [StudentProfileId], [DayOfWeek], [CreatedAt]) VALUES (1, 1, 2, SYSUTCDATETIME());
INSERT INTO [StudentCourseDays] ([Id], [StudentProfileId], [DayOfWeek], [CreatedAt]) VALUES (2, 1, 4, SYSUTCDATETIME());
INSERT INTO [StudentCourseDays] ([Id], [StudentProfileId], [DayOfWeek], [CreatedAt]) VALUES (3, 2, 1, SYSUTCDATETIME());
INSERT INTO [StudentCourseDays] ([Id], [StudentProfileId], [DayOfWeek], [CreatedAt]) VALUES (4, 3, 5, SYSUTCDATETIME());
SET IDENTITY_INSERT [StudentCourseDays] OFF;

SET IDENTITY_INSERT [MonthlyWorkPlans] ON;
INSERT INTO [MonthlyWorkPlans] ([Id], [StudentProfileId], [Year], [Month], [Status], [StudentNote], [LastSavedAt], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (1, 1, 2025, 9, N'Approved', N'Sınav dönemi yok.', CAST('2025-09-01T07:30:00' AS datetime2), 1, CAST('2025-09-02T08:00:00' AS datetime2), N'Onaylandı', SYSUTCDATETIME());
INSERT INTO [MonthlyWorkPlans] ([Id], [StudentProfileId], [Year], [Month], [Status], [StudentNote], [LastSavedAt], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (2, 1, 2025, 10, N'Approved', NULL, CAST('2025-10-01T07:30:00' AS datetime2), 1, CAST('2025-10-02T08:00:00' AS datetime2), NULL, SYSUTCDATETIME());
INSERT INTO [MonthlyWorkPlans] ([Id], [StudentProfileId], [Year], [Month], [Status], [StudentNote], [LastSavedAt], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (3, 1, 2026, 9, N'Approved', NULL, CAST('2026-09-01T07:30:00' AS datetime2), 1, CAST('2026-09-02T08:00:00' AS datetime2), NULL, SYSUTCDATETIME());
INSERT INTO [MonthlyWorkPlans] ([Id], [StudentProfileId], [Year], [Month], [Status], [StudentNote], [LastSavedAt], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (4, 1, 2026, 10, N'Approved', N'Salı ve perşembe derslerim var.', CAST('2026-10-01T07:30:00' AS datetime2), 1, CAST('2026-10-02T08:00:00' AS datetime2), NULL, SYSUTCDATETIME());
INSERT INTO [MonthlyWorkPlans] ([Id], [StudentProfileId], [Year], [Month], [Status], [StudentNote], [LastSavedAt], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (5, 2, 2026, 10, N'Pending', N'Onay bekliyor.', CAST('2026-10-01T07:30:00' AS datetime2), NULL, NULL, NULL, SYSUTCDATETIME());
INSERT INTO [MonthlyWorkPlans] ([Id], [StudentProfileId], [Year], [Month], [Status], [StudentNote], [LastSavedAt], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (6, 3, 2026, 10, N'Rejected', NULL, CAST('2026-10-01T07:30:00' AS datetime2), 1, CAST('2026-10-03T09:30:00' AS datetime2), N'Haftada en fazla 3 gün seçilebilir, planı yeniden düzenleyin.', SYSUTCDATETIME());
SET IDENTITY_INSERT [MonthlyWorkPlans] OFF;

SET IDENTITY_INSERT [WorkPlanDays] ON;
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (1, 1, CAST('2025-09-08' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (2, 1, CAST('2025-09-10' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (3, 1, CAST('2025-09-15' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (4, 1, CAST('2025-09-17' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (5, 1, CAST('2025-09-22' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (6, 1, CAST('2025-09-24' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (7, 2, CAST('2025-10-06' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (8, 2, CAST('2025-10-08' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (9, 2, CAST('2025-10-13' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (10, 2, CAST('2025-10-15' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (11, 2, CAST('2025-10-20' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (12, 2, CAST('2025-10-22' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (13, 2, CAST('2025-10-27' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (14, 3, CAST('2026-09-07' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (15, 3, CAST('2026-09-09' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (16, 3, CAST('2026-09-14' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (17, 3, CAST('2026-09-16' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (18, 3, CAST('2026-09-21' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (19, 3, CAST('2026-09-23' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (20, 4, CAST('2026-10-05' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (21, 4, CAST('2026-10-07' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (22, 4, CAST('2026-10-12' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (23, 4, CAST('2026-10-14' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (24, 4, CAST('2026-10-19' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (25, 4, CAST('2026-10-21' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (26, 4, CAST('2026-10-26' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (27, 4, CAST('2026-10-28' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (28, 5, CAST('2026-10-05' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (29, 5, CAST('2026-10-07' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (30, 5, CAST('2026-10-12' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (31, 5, CAST('2026-10-14' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (32, 5, CAST('2026-10-19' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (33, 5, CAST('2026-10-21' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (34, 6, CAST('2026-10-05' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (35, 6, CAST('2026-10-06' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (36, 6, CAST('2026-10-07' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (37, 6, CAST('2026-10-08' AS date), 0, NULL, SYSUTCDATETIME());
INSERT INTO [WorkPlanDays] ([Id], [MonthlyWorkPlanId], [Date], [IsLessonDay], [Note], [CreatedAt]) VALUES (38, 6, CAST('2026-10-09' AS date), 0, NULL, SYSUTCDATETIME());
SET IDENTITY_INSERT [WorkPlanDays] OFF;

SET IDENTITY_INSERT [LeaveRequests] ON;
INSERT INTO [LeaveRequests] ([Id], [StudentProfileId], [Type], [Status], [StartDate], [EndDate], [Hours], [DaysCounted], [Reason], [Description], [AppliedOn], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (1, 1, N'Excuse', N'Approved', CAST('2026-10-12' AS date), CAST('2026-10-12' AS date), NULL, 1, N'Ailevi mazeret', NULL, CAST('2026-10-08' AS date), 1, CAST('2026-10-09T10:00:00' AS datetime2), N'Uygun görüldü.', SYSUTCDATETIME());
INSERT INTO [LeaveRequests] ([Id], [StudentProfileId], [Type], [Status], [StartDate], [EndDate], [Hours], [DaysCounted], [Reason], [Description], [AppliedOn], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (2, 1, N'Excuse', N'Approved', CAST('2026-09-23' AS date), CAST('2026-09-23' AS date), 4, 0.5, N'Resmî işlem', NULL, CAST('2026-09-21' AS date), 1, CAST('2026-09-21T14:00:00' AS datetime2), NULL, SYSUTCDATETIME());
INSERT INTO [LeaveRequests] ([Id], [StudentProfileId], [Type], [Status], [StartDate], [EndDate], [Hours], [DaysCounted], [Reason], [Description], [AppliedOn], [ReviewedByUserId], [ReviewedAt], [ReviewNote], [CreatedAt]) VALUES (3, 2, N'Health', N'Pending', CAST('2026-10-26' AS date), CAST('2026-10-27' AS date), NULL, 2, N'Hastalık', N'Doktor raporu sonradan yüklenecek.', CAST('2026-10-21' AS date), NULL, NULL, NULL, SYSUTCDATETIME());
SET IDENTITY_INSERT [LeaveRequests] OFF;

SET IDENTITY_INSERT [Absences] ON;
INSERT INTO [Absences] ([Id], [StudentProfileId], [Date], [Type], [Note], [RecordedByUserId], [CreatedAt]) VALUES (1, 3, CAST('2026-10-14' AS date), N'Unexcused', N'Gelmedi, haber vermedi.', 1, SYSUTCDATETIME());
INSERT INTO [Absences] ([Id], [StudentProfileId], [Date], [Type], [Note], [RecordedByUserId], [CreatedAt]) VALUES (2, 5, CAST('2026-03-10' AS date), N'Unexcused', NULL, 1, SYSUTCDATETIME());
INSERT INTO [Absences] ([Id], [StudentProfileId], [Date], [Type], [Note], [RecordedByUserId], [CreatedAt]) VALUES (3, 5, CAST('2026-03-17' AS date), N'Unexcused', NULL, 1, SYSUTCDATETIME());
INSERT INTO [Absences] ([Id], [StudentProfileId], [Date], [Type], [Note], [RecordedByUserId], [CreatedAt]) VALUES (4, 5, CAST('2026-03-24' AS date), N'Unexcused', NULL, 1, SYSUTCDATETIME());
INSERT INTO [Absences] ([Id], [StudentProfileId], [Date], [Type], [Note], [RecordedByUserId], [CreatedAt]) VALUES (5, 5, CAST('2026-04-07' AS date), N'Unexcused', NULL, 1, SYSUTCDATETIME());
INSERT INTO [Absences] ([Id], [StudentProfileId], [Date], [Type], [Note], [RecordedByUserId], [CreatedAt]) VALUES (6, 5, CAST('2026-04-14' AS date), N'Unexcused', NULL, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [Absences] OFF;

SET IDENTITY_INSERT [AttendanceSessions] ON;
INSERT INTO [AttendanceSessions] ([Id], [Date], [StartTime], [EndTime], [Title], [DepartmentId], [SecretKey], [CreatedByUserId], [CreatedAt]) VALUES (1, CAST('2026-10-12' AS date), CAST('09:00:00' AS time), CAST('17:00:00' AS time), N'Günlük devam oturumu', NULL, N'ornek-gizli-anahtar-1', 1, SYSUTCDATETIME());
INSERT INTO [AttendanceSessions] ([Id], [Date], [StartTime], [EndTime], [Title], [DepartmentId], [SecretKey], [CreatedByUserId], [CreatedAt]) VALUES (2, CAST('2026-10-14' AS date), CAST('09:00:00' AS time), CAST('17:00:00' AS time), N'Günlük devam oturumu', NULL, N'ornek-gizli-anahtar-2', 1, SYSUTCDATETIME());
INSERT INTO [AttendanceSessions] ([Id], [Date], [StartTime], [EndTime], [Title], [DepartmentId], [SecretKey], [CreatedByUserId], [CreatedAt]) VALUES (3, CAST('2026-10-19' AS date), CAST('09:00:00' AS time), CAST('17:00:00' AS time), N'Günlük devam oturumu', NULL, N'ornek-gizli-anahtar-3', 1, SYSUTCDATETIME());
INSERT INTO [AttendanceSessions] ([Id], [Date], [StartTime], [EndTime], [Title], [DepartmentId], [SecretKey], [CreatedByUserId], [CreatedAt]) VALUES (4, CAST('2026-10-21' AS date), CAST('09:00:00' AS time), CAST('17:00:00' AS time), N'Günlük devam oturumu', NULL, N'ornek-gizli-anahtar-4', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [AttendanceSessions] OFF;

SET IDENTITY_INSERT [AttendanceRecords] ON;
INSERT INTO [AttendanceRecords] ([Id], [StudentProfileId], [AttendanceSessionId], [Date], [CheckedInAt], [WorkPlanDayId], [IsManual], [ManualReason], [CreatedByUserId], [CreatedAt]) VALUES (1, 1, 2, CAST('2026-10-14' AS date), CAST('2026-10-14T09:12:00' AS datetime2), 23, 0, NULL, NULL, SYSUTCDATETIME());
INSERT INTO [AttendanceRecords] ([Id], [StudentProfileId], [AttendanceSessionId], [Date], [CheckedInAt], [WorkPlanDayId], [IsManual], [ManualReason], [CreatedByUserId], [CreatedAt]) VALUES (2, 1, 3, CAST('2026-10-19' AS date), CAST('2026-10-19T09:12:00' AS datetime2), 24, 0, NULL, NULL, SYSUTCDATETIME());
INSERT INTO [AttendanceRecords] ([Id], [StudentProfileId], [AttendanceSessionId], [Date], [CheckedInAt], [WorkPlanDayId], [IsManual], [ManualReason], [CreatedByUserId], [CreatedAt]) VALUES (3, 1, 4, CAST('2026-10-21' AS date), CAST('2026-10-21T09:12:00' AS datetime2), 25, 0, NULL, NULL, SYSUTCDATETIME());
INSERT INTO [AttendanceRecords] ([Id], [StudentProfileId], [AttendanceSessionId], [Date], [CheckedInAt], [WorkPlanDayId], [IsManual], [ManualReason], [CreatedByUserId], [CreatedAt]) VALUES (4, 2, 3, CAST('2026-10-19' AS date), CAST('2026-10-19T09:12:00' AS datetime2), 32, 0, NULL, NULL, SYSUTCDATETIME());
INSERT INTO [AttendanceRecords] ([Id], [StudentProfileId], [AttendanceSessionId], [Date], [CheckedInAt], [WorkPlanDayId], [IsManual], [ManualReason], [CreatedByUserId], [CreatedAt]) VALUES (5, 2, 4, CAST('2026-10-21' AS date), CAST('2026-10-21T09:12:00' AS datetime2), 33, 0, NULL, NULL, SYSUTCDATETIME());
SET IDENTITY_INSERT [AttendanceRecords] OFF;

SET IDENTITY_INSERT [StudentTasks] ON;
INSERT INTO [StudentTasks] ([Id], [StudentProfileId], [Title], [Description], [AssignedDate], [DueDate], [AssignedByUserId], [Status], [StatusChangedAt], [CreatedAt]) VALUES (1, 1, N'Laboratuvar envanter sayımı', N'Bilgisayar laboratuvarındaki cihazların sayımı ve listesi.', CAST('2026-10-05' AS date), CAST('2026-10-16' AS date), 9, N'Completed', CAST('2026-10-15T16:00:00' AS datetime2), SYSUTCDATETIME());
INSERT INTO [StudentTasks] ([Id], [StudentProfileId], [Title], [Description], [AssignedDate], [DueDate], [AssignedByUserId], [Status], [StatusChangedAt], [CreatedAt]) VALUES (2, 1, N'Dönem sonu raporu hazırlığı', N'Dönem boyunca yapılan işlerin kısa özeti.', CAST('2026-10-19' AS date), CAST('2026-11-05' AS date), 9, N'Open', NULL, SYSUTCDATETIME());
SET IDENTITY_INSERT [StudentTasks] OFF;

SET IDENTITY_INSERT [TaskEvaluations] ON;
INSERT INTO [TaskEvaluations] ([Id], [StudentTaskId], [AuthorUserId], [Note], [NewStatus], [CreatedAt]) VALUES (1, 1, 9, N'Zamanında ve eksiksiz teslim edildi.', N'Completed', SYSUTCDATETIME());
SET IDENTITY_INSERT [TaskEvaluations] OFF;

SET IDENTITY_INSERT [AbsenceNotices] ON;
INSERT INTO [AbsenceNotices] ([Id], [StudentProfileId], [Date], [Message], [AdvisorUserId], [CreatedAt]) VALUES (1, 3, CAST('2026-10-14' AS date), N'Öğrenci bugün çalışma planına rağmen gelmedi.', 3, SYSUTCDATETIME());
SET IDENTITY_INSERT [AbsenceNotices] OFF;

SET IDENTITY_INSERT [TerminationDecisions] ON;
INSERT INTO [TerminationDecisions] ([Id], [StudentProfileId], [Basis], [DecisionDate], [Note], [LeaveDaysUsed], [UnexcusedAbsenceDays], [DecidedByUserId], [CreatedAt]) VALUES (1, 5, N'AbsenceLimitExceeded', CAST('2026-06-30' AS date), N'Devamsızlık sınırı (5 gün) aşıldığı için ilişik kesildi.', 0, 5, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [TerminationDecisions] OFF;

SET IDENTITY_INSERT [AuditLogs] ON;
INSERT INTO [AuditLogs] ([Id], [OccurredAt], [UserId], [UserEmail], [Action], [EntityName], [EntityId], [Details], [IpAddress]) VALUES (1, CAST('2026-10-09T10:00:00' AS datetime2), 1, N'sekreter@teknoloji.edu.tr', N'Leave.Approved', N'LeaveRequest', 1, NULL, N'10.0.0.5');
INSERT INTO [AuditLogs] ([Id], [OccurredAt], [UserId], [UserEmail], [Action], [EntityName], [EntityId], [Details], [IpAddress]) VALUES (2, CAST('2026-10-03T09:30:00' AS datetime2), 1, N'sekreter@teknoloji.edu.tr', N'WorkPlan.Rejected', N'MonthlyWorkPlan', 6, NULL, N'10.0.0.5');
INSERT INTO [AuditLogs] ([Id], [OccurredAt], [UserId], [UserEmail], [Action], [EntityName], [EntityId], [Details], [IpAddress]) VALUES (3, CAST('2026-06-30T11:00:00' AS datetime2), 1, N'sekreter@teknoloji.edu.tr', N'Termination.Recorded', N'TerminationDecision', 1, NULL, N'10.0.0.5');
SET IDENTITY_INSERT [AuditLogs] OFF;

COMMIT TRANSACTION;
PRINT N'Örnek veri yüklendi.';