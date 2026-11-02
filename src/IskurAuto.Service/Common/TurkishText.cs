using IskurAuto.Core.Enums;
using IskurAuto.Service.Reports;

namespace IskurAuto.Service.Common;

/// <summary>Denetim kaydı gibi kullanıcıya gösterilen kısa metinlerde kullanılan Türkçe adlar.</summary>
public static class TurkishText
{
    private static readonly string[] Months =
        { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

    public static string MonthYear(int year, int month) => $"{Months[month - 1]} {year}";

    public static string Role(UserRole role) => role switch
    {
        UserRole.Student => "Öğrenci",
        UserRole.Advisor => "Danışman öğretim elemanı",
        UserRole.DepartmentHead => "Bölüm başkanı",
        UserRole.FacultyAdmin => "Fakülte sekreterliği / dekanlık",
        _ => role.ToString()
    };

    public static string Report(ReportKind kind) => kind switch
    {
        ReportKind.MonthlyWork => "Aylık çalışma raporu",
        ReportKind.YearlyWork => "Yıllık çalışma raporu",
        ReportKind.LeaveAndAbsence => "İzin ve devamsızlık raporu",
        ReportKind.NearLeaveLimit => "İzin sınırına yaklaşanlar listesi",
        ReportKind.TerminationReview => "İlişik kesme inceleme listesi",
        ReportKind.Advisor => "Danışman özet raporu",
        ReportKind.StudentList => "Öğrenci listesi",
        ReportKind.MonthlyStatement => "Ay sonu dökümü",
        _ => kind.ToString()
    };
}
