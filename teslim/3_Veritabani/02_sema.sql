CREATE TABLE [AuditLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [OccurredAt] datetime2 NOT NULL,
    [UserId] int NULL,
    [UserEmail] nvarchar(254) NULL,
    [Action] nvarchar(100) NOT NULL,
    [EntityName] nvarchar(100) NOT NULL,
    [EntityId] int NULL,
    [Details] nvarchar(1000) NULL,
    [IpAddress] nvarchar(45) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ExamPeriods] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ExamPeriods] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Holidays] (
    [Id] int NOT NULL IDENTITY,
    [Date] date NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [WorkUnits] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_WorkUnits] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(200) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] nvarchar(30) NOT NULL,
    [DepartmentId] int NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AttendanceSessions] (
    [Id] int NOT NULL IDENTITY,
    [Date] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Title] nvarchar(150) NULL,
    [DepartmentId] int NULL,
    [SecretKey] nvarchar(100) NOT NULL,
    [CreatedByUserId] int NOT NULL,
    [ClosedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_AttendanceSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceSessions_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceSessions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PasswordResetTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [UsedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudentProfiles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [NationalId] nchar(11) NOT NULL,
    [Iban] nvarchar(26) NOT NULL,
    [StudentNumber] nvarchar(20) NOT NULL,
    [DepartmentId] int NOT NULL,
    [RegistrationYear] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_StudentProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentProfiles_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AbsenceNotices] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [Date] date NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [AdvisorUserId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    [ReadByUserId] int NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_AbsenceNotices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AbsenceNotices_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AbsenceNotices_Users_AdvisorUserId] FOREIGN KEY ([AdvisorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AbsenceNotices_Users_ReadByUserId] FOREIGN KEY ([ReadByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Absences] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [Date] date NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Note] nvarchar(500) NULL,
    [RecordedByUserId] int NOT NULL,
    [RevokedAt] datetime2 NULL,
    [RevokedByUserId] int NULL,
    [RevokeReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Absences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Absences_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Absences_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Absences_Users_RevokedByUserId] FOREIGN KEY ([RevokedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AdvisorAssignments] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [AdvisorUserId] int NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [AssignedByUserId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_AdvisorAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdvisorAssignments_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvisorAssignments_Users_AdvisorUserId] FOREIGN KEY ([AdvisorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvisorAssignments_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LeaveRequests] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [Hours] decimal(5,2) NULL,
    [DaysCounted] decimal(5,2) NOT NULL,
    [Reason] nvarchar(300) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [AppliedOn] date NOT NULL,
    [ReviewedByUserId] int NULL,
    [ReviewedAt] datetime2 NULL,
    [ReviewNote] nvarchar(1000) NULL,
    [DocumentFileName] nvarchar(255) NULL,
    [DocumentStoredName] nvarchar(100) NULL,
    [DocumentContentType] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveRequests_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaveRequests_Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MonthlyWorkPlans] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [Year] int NOT NULL,
    [Month] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [StudentNote] nvarchar(1000) NULL,
    [LastSavedAt] datetime2 NOT NULL,
    [ReviewedByUserId] int NULL,
    [ReviewedAt] datetime2 NULL,
    [ReviewNote] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_MonthlyWorkPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MonthlyWorkPlans_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MonthlyWorkPlans_Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProgramParticipations] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [EndReason] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ProgramParticipations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProgramParticipations_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudentCourseDays] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_StudentCourseDays] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentCourseDays_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudentTasks] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [AssignedDate] date NOT NULL,
    [DueDate] date NOT NULL,
    [AssignedByUserId] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [StatusChangedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_StudentTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentTasks_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentTasks_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudentUnitAssignments] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [WorkUnitId] int NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [AssignedByUserId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_StudentUnitAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentUnitAssignments_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentUnitAssignments_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentUnitAssignments_WorkUnits_WorkUnitId] FOREIGN KEY ([WorkUnitId]) REFERENCES [WorkUnits] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TerminationDecisions] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [Basis] nvarchar(30) NOT NULL,
    [DecisionDate] date NOT NULL,
    [Note] nvarchar(1000) NOT NULL,
    [LeaveDaysUsed] decimal(5,2) NOT NULL,
    [UnexcusedAbsenceDays] int NOT NULL,
    [DecidedByUserId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_TerminationDecisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TerminationDecisions_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TerminationDecisions_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkPlanDays] (
    [Id] int NOT NULL IDENTITY,
    [MonthlyWorkPlanId] int NOT NULL,
    [Date] date NOT NULL,
    [IsLessonDay] bit NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_WorkPlanDays] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkPlanDays_MonthlyWorkPlans_MonthlyWorkPlanId] FOREIGN KEY ([MonthlyWorkPlanId]) REFERENCES [MonthlyWorkPlans] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TaskEvaluations] (
    [Id] int NOT NULL IDENTITY,
    [StudentTaskId] int NOT NULL,
    [AuthorUserId] int NOT NULL,
    [Note] nvarchar(2000) NULL,
    [NewStatus] nvarchar(20) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_TaskEvaluations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskEvaluations_StudentTasks_StudentTaskId] FOREIGN KEY ([StudentTaskId]) REFERENCES [StudentTasks] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaskEvaluations_Users_AuthorUserId] FOREIGN KEY ([AuthorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AttendanceRecords] (
    [Id] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [AttendanceSessionId] int NOT NULL,
    [Date] date NOT NULL,
    [CheckedInAt] datetime2 NOT NULL,
    [WorkPlanDayId] int NULL,
    [IsManual] bit NOT NULL,
    [ManualReason] nvarchar(500) NULL,
    [CreatedByUserId] int NULL,
    [VoidedAt] datetime2 NULL,
    [VoidedByUserId] int NULL,
    [VoidReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRecords_AttendanceSessions_AttendanceSessionId] FOREIGN KEY ([AttendanceSessionId]) REFERENCES [AttendanceSessions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRecords_StudentProfiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [StudentProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRecords_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRecords_Users_VoidedByUserId] FOREIGN KEY ([VoidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRecords_WorkPlanDays_WorkPlanDayId] FOREIGN KEY ([WorkPlanDayId]) REFERENCES [WorkPlanDays] ([Id]) ON DELETE SET NULL
);
GO


CREATE UNIQUE INDEX [IX_AbsenceNotices_AdvisorUserId_StudentProfileId_Date] ON [AbsenceNotices] ([AdvisorUserId], [StudentProfileId], [Date]);
GO


CREATE INDEX [IX_AbsenceNotices_ReadByUserId] ON [AbsenceNotices] ([ReadByUserId]);
GO


CREATE INDEX [IX_AbsenceNotices_StudentProfileId] ON [AbsenceNotices] ([StudentProfileId]);
GO


CREATE INDEX [IX_Absences_RecordedByUserId] ON [Absences] ([RecordedByUserId]);
GO


CREATE INDEX [IX_Absences_RevokedByUserId] ON [Absences] ([RevokedByUserId]);
GO


CREATE UNIQUE INDEX [UX_Absences_OneActivePerStudentDay] ON [Absences] ([StudentProfileId], [Date]) WHERE [RevokedAt] IS NULL;
GO


CREATE INDEX [IX_AdvisorAssignments_AdvisorUserId] ON [AdvisorAssignments] ([AdvisorUserId]);
GO


CREATE INDEX [IX_AdvisorAssignments_AssignedByUserId] ON [AdvisorAssignments] ([AssignedByUserId]);
GO


CREATE UNIQUE INDEX [UX_AdvisorAssignments_OneOpenPerStudent] ON [AdvisorAssignments] ([StudentProfileId]) WHERE [EndDate] IS NULL;
GO


CREATE INDEX [IX_AttendanceRecords_AttendanceSessionId] ON [AttendanceRecords] ([AttendanceSessionId]);
GO


CREATE INDEX [IX_AttendanceRecords_CreatedByUserId] ON [AttendanceRecords] ([CreatedByUserId]);
GO


CREATE INDEX [IX_AttendanceRecords_StudentProfileId_Date] ON [AttendanceRecords] ([StudentProfileId], [Date]);
GO


CREATE INDEX [IX_AttendanceRecords_VoidedByUserId] ON [AttendanceRecords] ([VoidedByUserId]);
GO


CREATE INDEX [IX_AttendanceRecords_WorkPlanDayId] ON [AttendanceRecords] ([WorkPlanDayId]);
GO


CREATE UNIQUE INDEX [UX_Attendance_OneActivePerStudentSession] ON [AttendanceRecords] ([StudentProfileId], [AttendanceSessionId]) WHERE [VoidedAt] IS NULL;
GO


CREATE INDEX [IX_AttendanceSessions_CreatedByUserId] ON [AttendanceSessions] ([CreatedByUserId]);
GO


CREATE INDEX [IX_AttendanceSessions_Date] ON [AttendanceSessions] ([Date]);
GO


CREATE INDEX [IX_AttendanceSessions_DepartmentId] ON [AttendanceSessions] ([DepartmentId]);
GO


CREATE INDEX [IX_AuditLogs_OccurredAt] ON [AuditLogs] ([OccurredAt]);
GO


CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Departments_Code] ON [Departments] ([Code]);
GO


CREATE UNIQUE INDEX [IX_Holidays_Date] ON [Holidays] ([Date]);
GO


CREATE INDEX [IX_LeaveRequests_ReviewedByUserId] ON [LeaveRequests] ([ReviewedByUserId]);
GO


CREATE INDEX [IX_LeaveRequests_StudentProfileId_StartDate] ON [LeaveRequests] ([StudentProfileId], [StartDate]);
GO


CREATE INDEX [IX_MonthlyWorkPlans_ReviewedByUserId] ON [MonthlyWorkPlans] ([ReviewedByUserId]);
GO


CREATE UNIQUE INDEX [IX_MonthlyWorkPlans_StudentProfileId_Year_Month] ON [MonthlyWorkPlans] ([StudentProfileId], [Year], [Month]);
GO


CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
GO


CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
GO


CREATE UNIQUE INDEX [UX_ProgramParticipations_OneOpenPerStudent] ON [ProgramParticipations] ([StudentProfileId]) WHERE [EndDate] IS NULL;
GO


CREATE UNIQUE INDEX [IX_StudentCourseDays_StudentProfileId_DayOfWeek] ON [StudentCourseDays] ([StudentProfileId], [DayOfWeek]);
GO


CREATE INDEX [IX_StudentProfiles_DepartmentId] ON [StudentProfiles] ([DepartmentId]);
GO


CREATE UNIQUE INDEX [IX_StudentProfiles_NationalId] ON [StudentProfiles] ([NationalId]);
GO


CREATE UNIQUE INDEX [IX_StudentProfiles_StudentNumber] ON [StudentProfiles] ([StudentNumber]);
GO


CREATE UNIQUE INDEX [IX_StudentProfiles_UserId] ON [StudentProfiles] ([UserId]);
GO


CREATE INDEX [IX_StudentTasks_AssignedByUserId] ON [StudentTasks] ([AssignedByUserId]);
GO


CREATE INDEX [IX_StudentTasks_StudentProfileId_Status] ON [StudentTasks] ([StudentProfileId], [Status]);
GO


CREATE INDEX [IX_StudentUnitAssignments_AssignedByUserId] ON [StudentUnitAssignments] ([AssignedByUserId]);
GO


CREATE INDEX [IX_StudentUnitAssignments_WorkUnitId] ON [StudentUnitAssignments] ([WorkUnitId]);
GO


CREATE UNIQUE INDEX [UX_StudentUnitAssignments_OneOpenPerStudent] ON [StudentUnitAssignments] ([StudentProfileId]) WHERE [EndDate] IS NULL;
GO


CREATE UNIQUE INDEX [IX_SystemSettings_Key] ON [SystemSettings] ([Key]);
GO


CREATE INDEX [IX_TaskEvaluations_AuthorUserId] ON [TaskEvaluations] ([AuthorUserId]);
GO


CREATE INDEX [IX_TaskEvaluations_StudentTaskId] ON [TaskEvaluations] ([StudentTaskId]);
GO


CREATE INDEX [IX_TerminationDecisions_DecidedByUserId] ON [TerminationDecisions] ([DecidedByUserId]);
GO


CREATE INDEX [IX_TerminationDecisions_StudentProfileId_DecisionDate] ON [TerminationDecisions] ([StudentProfileId], [DecisionDate]);
GO


CREATE INDEX [IX_Users_DepartmentId] ON [Users] ([DepartmentId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE UNIQUE INDEX [IX_WorkPlanDays_MonthlyWorkPlanId_Date] ON [WorkPlanDays] ([MonthlyWorkPlanId], [Date]);
GO


CREATE UNIQUE INDEX [IX_WorkUnits_Name] ON [WorkUnits] ([Name]);
GO


