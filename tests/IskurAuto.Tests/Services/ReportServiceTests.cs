using System.Text;
using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Reports;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

/// <summary>
/// Saat sabit: 21 Ekim 2026, yerel 12:00. Mehmet'in onaylı Ekim planı 1, 6, 14, 21, 28;
/// 21 Ekim'de karekodla devam kaydı var. Elif'in planı yoktur.
/// </summary>
public class ReportServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);
    private static readonly ReportQuery October = new() { Year = 2026, Month = 10 };

    private sealed class Fixture : IDisposable
    {
        public required TestDb Db { get; init; }
        public required SeedResult Seed { get; init; }
        public void Dispose() => Db.Dispose();
    }

    private static async Task<Fixture> CreateAsync()
    {
        var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            var plan = await scope.WorkPlans.SaveForStudentAsync(seed.MehmetId, new SaveWorkPlanRequest
            {
                Year = 2026, Month = 10,
                Days = new[] { 1, 6, 14, 21, 28 }.Select(d => new WorkPlanDayInput { Date = new DateOnly(2026, 10, d) }).ToList()
            });
            await scope.WorkPlans.ReviewAsync(plan.Id, new ReviewWorkPlanRequest { Approve = true });
        }

        SessionDto session;
        using (var scope = db.NewScope())
            session = await scope.Attendance.CreateSessionAsync(new CreateSessionRequest
            {
                Date = Today, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(14, 0)
            });
        string token;
        using (var scope = db.NewScope())
            token = (await scope.Attendance.GetCurrentQrAsync(session.Id)).Token;

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
            await scope.Attendance.CheckInAsync(new CheckInRequest { Token = token });

        return new Fixture { Db = db, Seed = seed };
    }

    private static async Task ApproveLeaveAsync(Fixture f, DateOnly from, DateOnly to)
    {
        f.Db.Current.As(UserRole.Student, f.Seed.MehmetUserId);
        LeaveResultDto requested;
        using (var scope = f.Db.NewScope())
            requested = await scope.Leaves.RequestAsync(new CreateLeaveRequest
            {
                Type = LeaveType.Excuse, StartDate = from, EndDate = to, Reason = "Aile işi"
            });

        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
            await scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true });
    }

    private static async Task<ReportTable> ReportAsync(Fixture f, ReportKind kind, ReportQuery query, UserRole role = UserRole.FacultyAdmin)
    {
        f.Db.Current.As(role, role switch
        {
            UserRole.DepartmentHead => f.Seed.HeadId,
            UserRole.Advisor => f.Seed.AdvisorId,
            _ => f.Seed.AdminId
        }, role is UserRole.FacultyAdmin ? null : f.Seed.BmId);
        using var scope = f.Db.NewScope();
        return await scope.Reports.GetAsync(kind, query);
    }

    private static object? Cell(ReportTable table, string rowKey, string column, int keyColumn = 1)
    {
        var row = table.Rows.Single(r => Equals(r[keyColumn], rowKey));
        return row[table.Columns.ToList().IndexOf(column)];
    }

    private static string[] Names(ReportTable table, int column = 1) => table.Rows.Select(r => (string)r[column]!).ToArray();

    // ─── Aylık çalışma ───────────────────────────────────────────────────────

    [Fact]
    public async Task MonthlyWork_ShowsPlannedVersusActual_AndATotalRow()
    {
        using var f = await CreateAsync();

        var table = await ReportAsync(f, ReportKind.MonthlyWork, October);

        Assert.Equal(new[] { "Elif Yıldız", "Mehmet Öztürk", "TOPLAM" }, Names(table));
        Assert.Equal(5m, Cell(table, "Mehmet Öztürk", "Planlanan Gün"));
        Assert.Equal(1m, Cell(table, "Mehmet Öztürk", "Gerçekleşen Gün"));
        Assert.Equal(3m, Cell(table, "Mehmet Öztürk", "Kayıt Yok"));       // 1, 6 ve 14 Ekim
        Assert.Equal(8m, Cell(table, "Mehmet Öztürk", "Çalışma Saati"));
        Assert.Equal(0m, Cell(table, "Elif Yıldız", "Çalışma Saati"));
        Assert.Equal(8m, Cell(table, "TOPLAM", "Çalışma Saati"));
    }

    [Fact]
    public async Task MonthlyWork_CountsApprovedLeaveAndRecordedAbsence()
    {
        using var f = await CreateAsync();
        await ApproveLeaveAsync(f, new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 14));
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using (var scope = f.Db.NewScope())
            await scope.Absences.RecordAsync(new RecordAbsenceRequest { StudentId = f.Seed.MehmetId, Date = new DateOnly(2026, 10, 6), Type = AbsenceType.Unexcused });

        var table = await ReportAsync(f, ReportKind.MonthlyWork, October);

        Assert.Equal(1m, Cell(table, "Mehmet Öztürk", "İzinli Gün"));
        Assert.Equal(1m, Cell(table, "Mehmet Öztürk", "Kayıtlı Devamsızlık"));
        Assert.Equal(1m, Cell(table, "Mehmet Öztürk", "Kayıt Yok"));       // yalnızca 1 Ekim kaldı
    }

    [Fact]
    public async Task MonthlyWork_FiltersByStatusDepartmentAndStudent()
    {
        using var f = await CreateAsync();

        var ym = await ReportAsync(f, ReportKind.MonthlyWork, new ReportQuery { Year = 2026, Month = 10, DepartmentId = f.Seed.YmId });
        Assert.Equal(new[] { "Elif Yıldız", "TOPLAM" }, Names(ym));

        var one = await ReportAsync(f, ReportKind.MonthlyWork, new ReportQuery { Year = 2026, Month = 10, StudentId = f.Seed.MehmetId });
        Assert.Equal(new[] { "Mehmet Öztürk", "TOPLAM" }, Names(one));

        using (var ctx = f.Db.NewContext())
        {
            (await ctx.ProgramParticipations.SingleAsync(p => p.StudentProfileId == f.Seed.ElifId)).EndDate = Today;
            await ctx.SaveChangesAsync();
        }
        var passive = await ReportAsync(f, ReportKind.MonthlyWork, new ReportQuery { Year = 2026, Month = 10, IsActive = false });
        Assert.Equal(new[] { "Elif Yıldız", "TOPLAM" }, Names(passive));
    }

    [Fact]
    public async Task Reports_FollowTheRoleScope()
    {
        using var f = await CreateAsync();
        var anotherDepartment = new ReportQuery { Year = 2026, Month = 10, DepartmentId = f.Seed.YmId };

        var head = await ReportAsync(f, ReportKind.MonthlyWork, anotherDepartment, UserRole.DepartmentHead);
        Assert.Equal(new[] { "Mehmet Öztürk", "TOPLAM" }, Names(head));      // istenen bölüm kapsamı aşamaz

    }

    [Fact]
    public async Task Advisors_CannotOpenReports()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.Advisor, f.Seed.AdvisorId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Reports.GetAsync(ReportKind.MonthlyWork, October));
    }

    [Fact]
    public async Task Students_CannotOpenReports()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.Student, f.Seed.MehmetUserId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Reports.GetAsync(ReportKind.MonthlyWork, October));
    }

    [Fact]
    public async Task MissingYearOrMonth_IsReported()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);
        using var scope = f.Db.NewScope();

        var error = await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Reports.GetAsync(ReportKind.MonthlyWork, new ReportQuery()));
        Assert.True(error.Errors.ContainsKey("Year"));
        Assert.True(error.Errors.ContainsKey("Month"));
    }

    // ─── Yıllık ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task YearlyWork_ListsHoursPerMonthAndTheYearTotal()
    {
        using var f = await CreateAsync();

        var table = await ReportAsync(f, ReportKind.YearlyWork, new ReportQuery { Year = 2026 });

        Assert.Equal(8m, Cell(table, "Mehmet Öztürk", "Eki (saat)"));
        Assert.Equal(0m, Cell(table, "Mehmet Öztürk", "Kas (saat)"));
        Assert.Equal(8m, Cell(table, "Mehmet Öztürk", "Toplam Saat"));
        Assert.Equal(1m, Cell(table, "Mehmet Öztürk", "Gerçekleşen Gün"));
    }

    // ─── İzin, devamsızlık, sınır ve ilişik kesme listeleri ──────────────────

    [Fact]
    public async Task NearLimit_AndTerminationReview_SplitStudentsByHowFarTheyAre()
    {
        using var f = await CreateAsync();
        var year = new ReportQuery { Year = 2026 };

        Assert.Empty((await ReportAsync(f, ReportKind.NearLeaveLimit, year)).Rows);

        await ApproveLeaveAsync(f, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 12));   // 9 hafta içi günü: sınıra yakın
        var near = await ReportAsync(f, ReportKind.NearLeaveLimit, year);
        Assert.Equal("Mehmet Öztürk", Assert.Single(near.Rows)[1]);
        Assert.Contains("bitmek üzere", (string)Cell(near, "Mehmet Öztürk", "Uyarı")!);
        Assert.Empty((await ReportAsync(f, ReportKind.TerminationReview, year)).Rows);

        await ApproveLeaveAsync(f, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 17));  // toplam 11 gün: sınır aşıldı
        var review = await ReportAsync(f, ReportKind.TerminationReview, year);
        Assert.Equal("Mehmet Öztürk", Assert.Single(review.Rows)[1]);
        Assert.Contains("aşıldı", (string)Cell(review, "Mehmet Öztürk", "Uyarı")!);
        Assert.Equal(11m, Cell(review, "Mehmet Öztürk", "Kullanılan"));

        var all = await ReportAsync(f, ReportKind.LeaveAndAbsence, year);
        Assert.Equal(2, all.Rows.Count);    // izin kullanmayan öğrenci de listelenir
    }

    [Fact]
    public async Task TerminationReview_IsNotForAdvisors()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.Advisor, f.Seed.AdvisorId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Reports.GetAsync(ReportKind.TerminationReview, new ReportQuery { Year = 2026 }));
    }

    // ─── Danışman ve öğrenci listeleri ───────────────────────────────────────

    [Fact]
    public async Task AdvisorReport_CountsStudentsAndTasksPerAdvisor()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.Advisor, f.Seed.AdvisorId, f.Seed.BmId);
        using (var scope = f.Db.NewScope())
        {
            await scope.Tasks.AssignAsync(new AssignTaskRequest { StudentId = f.Seed.MehmetId, Title = "Envanter", DueDate = Today.AddDays(3) });
            var done = await scope.Tasks.AssignAsync(new AssignTaskRequest { StudentId = f.Seed.MehmetId, Title = "Rapor", DueDate = Today.AddDays(3) });
            await scope.Tasks.EvaluateAsync(done.Id, new EvaluateTaskRequest { NewStatus = StudentTaskStatus.Completed });
        }

        var table = await ReportAsync(f, ReportKind.Advisor, new ReportQuery { Year = 2026 });

        Assert.Equal(1, Cell(table, "Zeynep Arslan", "Aktif Öğrenci", keyColumn: 0));
        Assert.Equal(1, Cell(table, "Zeynep Arslan", "Açık Görev", keyColumn: 0));
        Assert.Equal(1, Cell(table, "Zeynep Arslan", "Tamamlanan", keyColumn: 0));
        Assert.Equal(1, Cell(table, "Danışman atanmamış", "Aktif Öğrenci", keyColumn: 0));
    }

    [Fact]
    public async Task StudentList_FiltersActiveAndPassive()
    {
        using var f = await CreateAsync();
        using (var ctx = f.Db.NewContext())
        {
            (await ctx.ProgramParticipations.SingleAsync(p => p.StudentProfileId == f.Seed.ElifId)).EndDate = Today;
            await ctx.SaveChangesAsync();
        }

        Assert.Equal(new[] { "Mehmet Öztürk" }, Names(await ReportAsync(f, ReportKind.StudentList, new ReportQuery { IsActive = true })));
        Assert.Equal(new[] { "Elif Yıldız" }, Names(await ReportAsync(f, ReportKind.StudentList, new ReportQuery { IsActive = false })));
        Assert.Equal(2, (await ReportAsync(f, ReportKind.StudentList, new ReportQuery())).Rows.Count);
    }

    // ─── Aylık döküm ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MonthlyStatement_HasNationalIdAndIban_ForStudentsWhoWorkedOrWerePlanned()
    {
        using var f = await CreateAsync();

        var table = await ReportAsync(f, ReportKind.MonthlyStatement, October);

        var row = Assert.Single(table.Rows);
        Assert.Equal("12345678950", row[0]);
        Assert.Equal("Mehmet Öztürk", row[1]);
        Assert.Equal("TR400006200000000000000001", row[2]);
        Assert.Equal("Bilgisayar Mühendisliği", row[3]);
        Assert.Equal("Bilgisayar Laboratuvarı", row[4]);
        Assert.Equal("Ekim 2026", row[5]);
        Assert.Equal(8m, row[9]);

        using var verify = f.Db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.ReportViewed));
    }

    [Theory]
    [InlineData(UserRole.DepartmentHead)]
    [InlineData(UserRole.Advisor)]
    public async Task MonthlyStatement_IsOnlyForTheFacultyOffice(UserRole role)
    {
        using var f = await CreateAsync();
        f.Db.Current.As(role, f.Seed.HeadId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Reports.GetAsync(ReportKind.MonthlyStatement, October));
    }

    // ─── Dışa aktarma ────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_ProducesCsvAndXlsx_AndIsAudited()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.FacultyAdmin, f.Seed.AdminId);

        using (var scope = f.Db.NewScope())
        {
            var csv = await scope.Reports.ExportAsync(ReportKind.MonthlyWork, October, ReportFormat.Csv);
            Assert.Equal("MonthlyWork-2026-10.csv", csv.FileName);
            Assert.Contains("Mehmet Öztürk", Encoding.UTF8.GetString(csv.Content));

            var xlsx = await scope.Reports.ExportAsync(ReportKind.MonthlyWork, October, ReportFormat.Xlsx);
            Assert.Equal("MonthlyWork-2026-10.xlsx", xlsx.FileName);
            Assert.Equal((byte)'P', xlsx.Content[0]);   // zip imzası "PK"
            Assert.Equal((byte)'K', xlsx.Content[1]);
        }

        using var verify = f.Db.NewContext();
        Assert.Equal(2, await verify.AuditLogs.CountAsync(a => a.Action == AuditActions.ReportExported));
    }

    [Fact]
    public async Task Export_AppliesTheSameScopeAsTheScreen()
    {
        using var f = await CreateAsync();
        f.Db.Current.As(UserRole.DepartmentHead, f.Seed.HeadId, f.Seed.BmId);
        using var scope = f.Db.NewScope();

        var csv = await scope.Reports.ExportAsync(ReportKind.MonthlyWork,
            new ReportQuery { Year = 2026, Month = 10, DepartmentId = f.Seed.YmId }, ReportFormat.Csv);

        var text = Encoding.UTF8.GetString(csv.Content);
        Assert.Contains("Mehmet Öztürk", text);
        Assert.DoesNotContain("Elif Yıldız", text);
    }
}
