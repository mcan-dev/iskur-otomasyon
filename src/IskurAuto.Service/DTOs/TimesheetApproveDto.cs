using System.ComponentModel.DataAnnotations;

namespace IskurAuto.Service.DTOs;

/// <summary>
/// SKS Admin'in (Checker) bir puantaj kaydını onaylarken veya reddederken
/// API'ye göndereceği veri transfer nesnesi.
/// </summary>
public class TimesheetApproveDto
{
    /// <summary>Onaylanacak/reddedilecek puantaj kaydının ID'si.</summary>
    [Required]
    public int TimesheetId { get; set; }

    /// <summary>
    /// Onay kararı.
    /// <c>true</c> → Onaylandı (ApprovedBySks).
    /// <c>false</c> → Reddedildi (PendingSksApproval statüsüne geri döner).
    /// </summary>
    [Required]
    public bool IsApproved { get; set; }

    /// <summary>SKS Admin'in onay veya red gerekçesi (isteğe bağlı).</summary>
    [MaxLength(1000)]
    public string? CheckerNote { get; set; }
}
