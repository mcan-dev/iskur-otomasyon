namespace IskurAuto.Core.Enums;

/// <summary>İlişik kesme kararının dayanağı. Karar her zaman yetkili bir kişi tarafından verilir.</summary>
public enum TerminationBasis
{
    LeaveLimitExceeded = 1,
    AbsenceLimitExceeded = 2,
    StudentRequest = 3,
    Other = 4
}
