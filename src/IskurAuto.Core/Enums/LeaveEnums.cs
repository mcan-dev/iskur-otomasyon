namespace IskurAuto.Core.Enums;

/// <summary>İzinler onay beklemez; kayıt ya geçerlidir ya da iptal edilmiştir.</summary>
public enum LeaveStatus
{
    Approved = 2,
    Cancelled = 4
}

public enum AbsenceType
{
    Unexcused = 1,
    HealthReport = 2,
    Excused = 3
}
