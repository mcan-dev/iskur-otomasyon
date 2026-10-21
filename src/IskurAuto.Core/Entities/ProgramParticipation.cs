namespace IskurAuto.Core.Entities;

/// <summary>
/// Öğrencinin İŞKUR programındaki bir katılım dönemi (belge madde 3).
/// Bir öğrenci farklı dönemlerde programa katılıp ayrılabilir; her dönem ayrı satırdır
/// ve ayrılma durumunda kayıt silinmez, sadece <see cref="EndDate"/> doldurulur (belge madde 9).
/// </summary>
public class ProgramParticipation : BaseEntity
{
    public int StudentProfileId { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>null ise katılım sürüyor (öğrenci aktif) demektir.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Programdan ayrılma gerekçesi / karar notu.</summary>
    public string? EndReason { get; set; }

    public bool IsOpen => EndDate is null;

    public StudentProfile StudentProfile { get; set; } = null!;
}
