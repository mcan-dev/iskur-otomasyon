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
}
