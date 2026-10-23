using System.Globalization;
using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Leaves;
using IskurAuto.Service.Reports;

namespace IskurAuto.Service.Implementations;

public class ReportService : IReportService
{
    private static readonly string[] MonthNames =
        { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

    private static readonly string[] ShortMonthNames =
        { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };

    private readonly IStudentRepository _students;
    private readonly IWorkPlanRepository _plans;
    private readonly IAttendanceRecordRepository _records;
    private readonly ILeaveRepository _leaves;
    private readonly IAbsenceRepository _absences;
    private readonly IStudentTaskRepository _tasks;
    private readonly ILeavePolicyProvider _policy;
    private readonly WorkPlanSettings _settings;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public ReportService(IStudentRepository students, IWorkPlanRepository plans, IAttendanceRecordRepository records,
        ILeaveRepository leaves, IAbsenceRepository absences, IStudentTaskRepository tasks, ILeavePolicyProvider policy,
        WorkPlanSettings settings, IUnitOfWork uow, IClock clock, IAuditService audit, ICurrentUser current)
    {
        _students = students;
        _plans = plans;
        _records = records;
        _leaves = leaves;
        _absences = absences;
        _tasks = tasks;
        _policy = policy;
        _settings = settings;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    public async Task<ReportTable> GetAsync(ReportKind kind, ReportQuery query, CancellationToken ct = default)
    {
        Authorize(kind);
        Validate(kind, query);

        var scope = BuildScope(kind, query);
        var students = await _students.ListAsync(scope, ct);

        var table = kind switch
        {
            ReportKind.MonthlyWork => await MonthlyWorkAsync(query, scope, students, ct),
            ReportKind.YearlyWork => await YearlyWorkAsync(query, scope, students, ct),
            ReportKind.LeaveAndAbsence => await BalanceReportAsync(kind, query, scope, students, _ => true, ct),
            ReportKind.NearLeaveLimit => await BalanceReportAsync(kind, query, scope, students, IsNearLimit, ct),
            ReportKind.TerminationReview => await BalanceReportAsync(kind, query, scope, students, IsOverLimit, ct),
            ReportKind.Advisor => await AdvisorReportAsync(query, scope, students, ct),
            ReportKind.StudentList => StudentList(students),
            ReportKind.MonthlyStatement => await MonthlyStatementAsync(query, scope, students, ct),
            _ => throw new DomainValidationException(nameof(kind), "Bilinmeyen rapor türü.")
        };

        if (kind == ReportKind.MonthlyStatement)
        {
            await _audit.RecordAsync(AuditActions.ReportViewed, nameof(ReportKind), details: $"{kind}; {table.Rows.Count} satır", ct: ct);
            await _uow.SaveChangesAsync(ct);
        }

        return table;
    }

    public async Task<ReportFile> ExportAsync(ReportKind kind, ReportQuery query, ReportFormat format, CancellationToken ct = default)
    {
        var table = await GetAsync(kind, query, ct);

        await _audit.RecordAsync(AuditActions.ReportExported, nameof(ReportKind),
            details: $"{kind}; {format}; {table.Rows.Count} satır", ct: ct);
        await _uow.SaveChangesAsync(ct);

        var period = query.Year is int year ? $"-{year}{(query.Month is int m ? $"-{m:00}" : "")}" : "";
        return format switch
        {
            ReportFormat.Csv => new ReportFile(ReportFileWriter.ToCsv(table), ReportFileWriter.CsvContentType, $"{kind}{period}.csv"),
            ReportFormat.Xlsx => new ReportFile(ReportFileWriter.ToXlsx(table), ReportFileWriter.XlsxContentType, $"{kind}{period}.xlsx"),
            _ => throw new DomainValidationException(nameof(format), "Desteklenmeyen dosya biçimi.")
        };
    }

    // ── Yetki ve kapsam ──────────────────────────────────────────────────────

    private void Authorize(ReportKind kind)
    {
        switch (kind)
        {
            case ReportKind.MonthlyStatement:    // T.C. kimlik no ve IBAN içerir
                _current.RequireRole(UserRole.FacultyAdmin);
                break;
            case ReportKind.TerminationReview:
                _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);
                break;
            default:
                _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);
                break;
        }
    }

    private static void Validate(ReportKind kind, ReportQuery query)
    {
        var errors = new ValidationErrors();

        var needsYear = kind is not ReportKind.StudentList;
        var needsMonth = kind is ReportKind.MonthlyWork or ReportKind.MonthlyStatement;

        if (needsYear && query.Year is null) errors.Add(nameof(query.Year), "Yıl seçmelisiniz.");
        if (needsMonth && query.Month is null) errors.Add(nameof(query.Month), "Ay seçmelisiniz.");
        errors.ThrowIfAny();
    }

    /// <summary>Kullanıcının seçtiği filtreler, rolün kapsamını genişletemez; kapsam sonradan zorla yazılır.</summary>
    private StudentSearchCriteria BuildScope(ReportKind kind, ReportQuery query)
    {
        var scope = new StudentSearchCriteria
        {
            StudentId = query.StudentId,
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            IsActive = query.IsActive
        };

        switch (_current.Role)
        {
            case UserRole.DepartmentHead:
                scope.DepartmentId = _current.DepartmentId
                    ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış.");
                break;
            case UserRole.Advisor:
                scope.AdvisorUserId = _current.UserId;
                break;
        }

        if (kind is ReportKind.NearLeaveLimit or ReportKind.TerminationReview)
            scope.IsActive = true;

        return scope;
    }

    // ── Aylık ve yıllık çalışma ──────────────────────────────────────────────

    private async Task<ReportTable> MonthlyWorkAsync(ReportQuery query, StudentSearchCriteria scope,
        IReadOnlyList<StudentProfile> students, CancellationToken ct)
    {
        var (year, month) = (query.Year!.Value, query.Month!.Value);
        var figures = await LoadMonthAsync(year, month, scope, students, ct);

        var rows = students.Select(s =>
        {
            var f = figures[s.Id];
            return Row(s.StudentNumber, s.User.FullName, s.Department.Name, StatusText(s),
                f.PlannedDays, f.AttendedDays, f.LeaveDays, f.RecordedAbsenceDays, f.MissingDays, f.Hours);
        }).ToList();

        rows.Add(Row("", "TOPLAM", "", "",
            figures.Values.Sum(f => f.PlannedDays), figures.Values.Sum(f => f.AttendedDays),
            figures.Values.Sum(f => f.LeaveDays), figures.Values.Sum(f => f.RecordedAbsenceDays),
            figures.Values.Sum(f => f.MissingDays), figures.Values.Sum(f => f.Hours)));

        return new ReportTable
        {
            Title = $"Aylık çalışma raporu - {MonthNames[month - 1]} {year}",
            Columns = new[]
            {
                "Öğrenci No", "Ad Soyad", "Bölüm", "Durum", "Planlanan Gün", "Gerçekleşen Gün",
                "İzinli Gün", "Kayıtlı Devamsızlık", "Kayıt Yok", "Çalışma Saati"
            },
            Rows = rows
        };
    }

    private async Task<ReportTable> YearlyWorkAsync(ReportQuery query, StudentSearchCriteria scope,
        IReadOnlyList<StudentProfile> students, CancellationToken ct)
    {
        var year = query.Year!.Value;
        var months = new List<Dictionary<int, WorkFigures>>();
        for (var month = 1; month <= 12; month++)
            months.Add(await LoadMonthAsync(year, month, scope, students, ct));

        var rows = students.Select(s =>
        {
            var cells = new List<object?> { s.StudentNumber, s.User.FullName, s.Department.Name, StatusText(s) };
            cells.AddRange(months.Select(m => (object?)m[s.Id].Hours));
            cells.Add(months.Sum(m => m[s.Id].Hours));
            cells.Add(months.Sum(m => m[s.Id].AttendedDays));
            return (IReadOnlyList<object?>)cells;
        }).ToList();

        return new ReportTable
        {
            Title = $"Yıllık çalışma saati raporu - {year}",
            Columns = new[] { "Öğrenci No", "Ad Soyad", "Bölüm", "Durum" }
                .Concat(ShortMonthNames.Select(n => n + " (saat)"))
                .Concat(new[] { "Toplam Saat", "Gerçekleşen Gün" }).ToList(),
            Rows = rows
        };
    }

    private async Task<ReportTable> MonthlyStatementAsync(ReportQuery query, StudentSearchCriteria scope,
        IReadOnlyList<StudentProfile> students, CancellationToken ct)
    {
        var (year, month) = (query.Year!.Value, query.Month!.Value);
        var figures = await LoadMonthAsync(year, month, scope, students, ct);

        var rows = students
            .Where(s => figures[s.Id].PlannedDays > 0 || figures[s.Id].AttendedDays > 0)
            .Select(s =>
            {
                var f = figures[s.Id];
                return Row(s.NationalId, s.User.FullName, s.Iban, s.Department.Name,
                    s.CurrentUnitAssignment?.WorkUnit.Name ?? "", $"{MonthNames[month - 1]} {year}",
                    f.PlannedDays, f.AttendedDays, f.LeaveDays, f.Hours);
            }).ToList();

        return new ReportTable
        {
            Title = $"Aylık çalışma dökümü - {MonthNames[month - 1]} {year}",
            Columns = new[]
            {
                "T.C. Kimlik No", "Ad Soyad", "IBAN", "Bölüm", "Çalışma Birimi", "Dönem",
                "Planlanan Gün", "Gerçekleşen Gün", "İzinli Gün", "Toplam Çalışma Saati"
            },
            Rows = rows
        };
    }

    /// <summary>Verilen ay için her öğrencinin planlanan ve gerçekleşen çalışması; veri tek seferde toplu çekilir.</summary>
    private async Task<Dictionary<int, WorkFigures>> LoadMonthAsync(int year, int month, StudentSearchCriteria scope,
        IReadOnlyList<StudentProfile> students, CancellationToken ct)
    {
        var plans = (await _plans.SearchAsync(new WorkPlanSearchCriteria
        {
            Year = year, Month = month, StudentId = scope.StudentId, DepartmentId = scope.DepartmentId,
            AdvisorUserId = scope.AdvisorUserId, Status = WorkPlanStatus.Approved
        }, ct)).ToDictionary(p => p.StudentProfileId);

        var attended = (await _records.SearchAsync(new AttendanceSearchCriteria
        {
            Year = year, Month = month, StudentId = scope.StudentId, DepartmentId = scope.DepartmentId, AdvisorUserId = scope.AdvisorUserId
        }, ct)).ToLookup(r => r.StudentProfileId, r => r.Date);

        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var leaves = (await _leaves.SearchAsync(new LeaveSearchCriteria
        {
            Year = year, StudentId = scope.StudentId, DepartmentId = scope.DepartmentId,
            AdvisorUserId = scope.AdvisorUserId, Status = LeaveStatus.Approved
        }, ct)).Where(l => l.StartDate <= last && l.EndDate >= first).ToLookup(l => l.StudentProfileId);

        var absences = (await _absences.SearchAsync(new AbsenceSearchCriteria
        {
            Year = year, Month = month, StudentId = scope.StudentId, DepartmentId = scope.DepartmentId, AdvisorUserId = scope.AdvisorUserId
        }, ct)).Where(a => !a.IsRevoked).ToLookup(a => a.StudentProfileId, a => a.Date);

        var today = _clock.Today;
        return students.ToDictionary(s => s.Id, s => WorkFigures.Compute(
            plans.GetValueOrDefault(s.Id),
            attended[s.Id].ToHashSet(),
            leaves[s.Id],
            absences[s.Id].ToHashSet(),
            today,
            _settings.DailyHours));
    }

    // ── İzin, devamsızlık ve ilişik kesme ────────────────────────────────────

    private static bool IsNearLimit(LeaveBalance b)
        => b.Warnings.Any(w => w is LeaveWarning.LeaveNearLimit or LeaveWarning.LeavePendingExceeds);

    private static bool IsOverLimit(LeaveBalance b)
        => b.Warnings.Any(w => w is LeaveWarning.LeaveLimitExceeded);

    private async Task<ReportTable> BalanceReportAsync(ReportKind kind, ReportQuery query, StudentSearchCriteria scope,
        IReadOnlyList<StudentProfile> students, Func<LeaveBalance, bool> include, CancellationToken ct)
    {
        var year = query.Year!.Value;
        var policy = await _policy.GetAsync(ct);

        var leaves = (await _leaves.SearchAsync(new LeaveSearchCriteria
        {
            Year = year, StudentId = scope.StudentId, DepartmentId = scope.DepartmentId, AdvisorUserId = scope.AdvisorUserId
        }, ct)).ToLookup(l => l.StudentProfileId);
        var absences = (await _absences.SearchAsync(new AbsenceSearchCriteria
        {
            Year = year, StudentId = scope.StudentId, DepartmentId = scope.DepartmentId, AdvisorUserId = scope.AdvisorUserId
        }, ct)).ToLookup(a => a.StudentProfileId);

        var rows = students
            .Select(s => (Student: s, Balance: LeaveBalanceCalculator.Calculate(policy, leaves[s.Id], absences[s.Id], s.LeaveDayAllowance)))
            .Where(x => include(x.Balance))
            .Select(x => Row(x.Student.StudentNumber, x.Student.User.FullName, x.Student.Department.Name, StatusText(x.Student),
                x.Balance.Limit, x.Balance.Used, x.Balance.LeaveDays, x.Balance.AbsenceDays, x.Balance.Pending, x.Balance.Remaining,
                string.Join("; ", x.Balance.Warnings.Select(WarningText))))
            .ToList();

        var title = kind switch
        {
            ReportKind.NearLeaveLimit => $"İzin hakkı sınırına yaklaşan öğrenciler - {year}",
            ReportKind.TerminationReview => $"İlişik kesme değerlendirmesi gerektiren durumlar - {year}",
            _ => $"İzin ve devamsızlık raporu - {year}"
        };

        return new ReportTable
        {
            Title = title,
            Columns = new[]
            {
                "Öğrenci No", "Ad Soyad", "Bölüm", "Durum", "İzin Hakkı", "Kullanılan", "İzin Günü", "Devamsızlık Günü",
                "Bekleyen", "Kalan", "Uyarı"
            },
            Rows = rows
        };
    }

    private static string WarningText(LeaveWarning warning) => warning switch
    {
        LeaveWarning.LeaveNearLimit => "İzin hakkı bitmek üzere",
        LeaveWarning.LeavePendingExceeds => "Bekleyen talepler hakkı aşıyor",
        LeaveWarning.LeaveLimitExceeded => "İzin hakkı aşıldı",
        _ => warning.ToString()
    };

    // ── Danışman ve öğrenci listeleri ────────────────────────────────────────

    private async Task<ReportTable> AdvisorReportAsync(ReportQuery query, StudentSearchCriteria scope,
        IReadOnlyList<StudentProfile> students, CancellationToken ct)
    {
        var tasks = await _tasks.SearchAsync(new StudentTaskSearchCriteria
        {
            StudentId = scope.StudentId, DepartmentId = scope.DepartmentId, AdvisorUserId = scope.AdvisorUserId, Year = query.Year
        }, ct);

        var advisorOf = students.ToDictionary(s => s.Id, s => s.CurrentAdvisorAssignment?.AdvisorUser.FullName ?? "Danışman atanmamış");
        var tasksByAdvisor = tasks.Where(t => advisorOf.ContainsKey(t.StudentProfileId)).ToLookup(t => advisorOf[t.StudentProfileId]);
        var today = _clock.Today;

        var rows = students
            .GroupBy(s => advisorOf[s.Id])
            .OrderBy(g => g.Key, StringComparer.Create(new CultureInfo("tr-TR"), ignoreCase: true))
            .Select(g =>
            {
                var advisorTasks = tasksByAdvisor[g.Key].ToList();
                return Row(g.Key, g.Count(s => s.IsActive), g.Count(s => !s.IsActive),
                    advisorTasks.Count(t => t.Status == StudentTaskStatus.Open),
                    advisorTasks.Count(t => t.Status == StudentTaskStatus.Completed),
                    advisorTasks.Count(t => t.Status == StudentTaskStatus.NotCompleted),
                    advisorTasks.Count(t => t.Status == StudentTaskStatus.Open && t.DueDate < today));
            }).ToList();

        return new ReportTable
        {
            Title = "Danışman bazında öğrenci ve görev raporu",
            Columns = new[] { "Danışman", "Aktif Öğrenci", "Pasif Öğrenci", "Açık Görev", "Tamamlanan", "Tamamlanmayan", "Gecikmiş" },
            Rows = rows
        };
    }

    private static ReportTable StudentList(IReadOnlyList<StudentProfile> students)
    {
        var rows = students.Select(s =>
        {
            var latest = s.Participations.OrderByDescending(p => p.StartDate).FirstOrDefault();
            return Row(s.StudentNumber, s.User.FullName, s.Department.Name,
                s.CurrentUnitAssignment?.WorkUnit.Name ?? "",
                s.CurrentAdvisorAssignment?.AdvisorUser.FullName ?? "",
                s.RegistrationYear, latest?.StartDate, latest?.EndDate, StatusText(s));
        }).ToList();

        return new ReportTable
        {
            Title = "Öğrenci listesi",
            Columns = new[]
            {
                "Öğrenci No", "Ad Soyad", "Bölüm", "Çalışma Birimi", "Danışman", "Kayıt Yılı",
                "Program Başlangıç", "Program Bitiş", "Durum"
            },
            Rows = rows
        };
    }

    private static string StatusText(StudentProfile student) => student.IsActive ? "Aktif" : "Pasif";

    private static IReadOnlyList<object?> Row(params object?[] cells) => cells;
}
