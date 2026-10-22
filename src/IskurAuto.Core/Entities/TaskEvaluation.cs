using IskurAuto.Core.Enums;

namespace IskurAuto.Core.Entities;

/// <summary>
/// Danışmanın göreve eklediği not ve/veya durum değişikliği. Yalnızca eklenir; böylece
/// önceki değerlendirmeler ve durum değişiklikleri korunur.
/// </summary>
public class TaskEvaluation : BaseEntity
{
    public int StudentTaskId { get; set; }

    public int AuthorUserId { get; set; }

    public string? Note { get; set; }

    /// <summary>Bu kayıtla görev durumu değiştiyse yeni durum; yalnızca notsa null.</summary>
    public StudentTaskStatus? NewStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public StudentTask StudentTask { get; set; } = null!;
    public User AuthorUser { get; set; } = null!;
}
