namespace IskurAuto.Service.Reports;

public enum ReportKind
{
    /// <summary>Aylık çalışma: öğrenci, bölüm veya fakülte düzeyinde; planlanan ve gerçekleşen karşılaştırması ve toplam saat.</summary>
    MonthlyWork = 1,

    /// <summary>Yıllık çalışma saati: her öğrenci için ay ay ve toplam.</summary>
    YearlyWork = 2,

    LeaveAndAbsence = 3,

    /// <summary>Danışman bazında öğrenci ve görev özeti.</summary>
    Advisor = 4,

    /// <summary>Aktif ve pasif öğrenci listesi.</summary>
    StudentList = 5,

    NearLeaveLimit = 6,

    /// <summary>İlişik kesme değerlendirmesi gerektiren durumlar. Karar vermez, yalnızca inceleme listesi çıkarır.</summary>
    TerminationReview = 7,

    /// <summary>Ay sonu öğrenci bazında döküm (T.C. kimlik no ve IBAN içerir).</summary>
    MonthlyStatement = 8
}

public enum ReportFormat
{
    Csv = 1,
    Xlsx = 2
}

/// <summary>Ekranda gösterilen ve dosyaya aktarılan, tüm raporlar için ortak tablo biçimi.</summary>
public sealed class ReportTable
{
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = Array.Empty<IReadOnlyList<object?>>();
}

public sealed record ReportFile(byte[] Content, string ContentType, string FileName);
