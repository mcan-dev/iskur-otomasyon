namespace IskurAuto.Core.Constants;

/// <summary>
/// Denetim kaydında (AuditLog) kullanılan işlem adları.
/// Belge madde 11: "Kişisel verilerin görüntülenmesi, değiştirilmesi ve dışa aktarılması
/// gibi önemli işlemler kayıt altına alınmalıdır."
/// </summary>
public static class AuditActions
{
    public const string LoginSucceeded       = "Login.Succeeded";
    public const string LoginFailed          = "Login.Failed";
    public const string PasswordChanged      = "Password.Changed";

    public const string StudentRegistered    = "Student.Registered";
    public const string StudentProfileUpdated = "Student.ProfileUpdated";
    public const string PersonalDataViewed   = "Student.PersonalDataViewed";
    public const string ParticipationEnded   = "Participation.Ended";
    public const string ParticipationStarted = "Participation.Started";
    public const string UnitAssigned         = "Student.UnitAssigned";
    public const string AdvisorAssigned      = "Student.AdvisorAssigned";

    public const string StaffUserCreated     = "User.StaffCreated";
    public const string UserActivated        = "User.Activated";
    public const string UserDeactivated      = "User.Deactivated";

    public const string DepartmentCreated    = "Department.Created";
    public const string DepartmentUpdated    = "Department.Updated";
    public const string WorkUnitCreated      = "WorkUnit.Created";
    public const string WorkUnitUpdated      = "WorkUnit.Updated";

    public const string WorkPlanSaved        = "WorkPlan.Saved";
    public const string WorkPlanApproved     = "WorkPlan.Approved";
    public const string WorkPlanRejected     = "WorkPlan.Rejected";
    public const string CourseDaysUpdated    = "Student.CourseDaysUpdated";
    public const string HolidayCreated       = "Holiday.Created";
    public const string HolidayDeleted       = "Holiday.Deleted";
    public const string ExamPeriodCreated    = "ExamPeriod.Created";
    public const string ExamPeriodDeleted    = "ExamPeriod.Deleted";

    public const string LeaveRequested       = "Leave.Requested";
    public const string LeaveCancelled       = "Leave.Cancelled";
    public const string LeaveApproved        = "Leave.Approved";
    public const string LeaveRejected        = "Leave.Rejected";
    public const string LeaveDocumentAdded   = "Leave.DocumentAdded";
    public const string LeaveDocumentViewed  = "Leave.DocumentViewed";
    public const string AbsenceRecorded      = "Absence.Recorded";
    public const string AbsenceRevoked       = "Absence.Revoked";
    public const string LeavePolicyChanged   = "Settings.LeavePolicyChanged";
}
