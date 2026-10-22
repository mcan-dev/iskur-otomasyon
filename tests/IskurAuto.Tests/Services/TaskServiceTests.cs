using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class TaskServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    private static AssignTaskRequest Task(int studentId, int dueInDays = 7, string title = "Laboratuvar envanteri") => new()
    {
        StudentId = studentId,
        Title = title,
        Description = "Cihazların listesini çıkar",
        DueDate = Today.AddDays(dueInDays)
    };

    private static async Task<TaskDto> AssignAsAdvisorAsync(TestDb db, SeedResult seed, int dueInDays = 7)
    {
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using var scope = db.NewScope();
        return await scope.Tasks.AssignAsync(Task(seed.MehmetId, dueInDays));
    }

    // ─── Atama ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_ByTheStudentsAdvisor_CreatesAnOpenTask()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        var task = await AssignAsAdvisorAsync(db, seed);

        Assert.Equal(StudentTaskStatus.Open, task.Status);
        Assert.Equal(Today, task.AssignedDate);
        Assert.Equal(Today.AddDays(7), task.DueDate);
        Assert.Equal("Zeynep Arslan", task.AssignedByName);
        Assert.False(task.IsOverdue);
        Assert.Empty(task.Evaluations);
    }

    [Fact]
    public async Task Assign_ByDepartmentHeadAndAdmin_IsAllowedInScope()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Tasks.AssignAsync(Task(seed.MehmetId));

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Tasks.AssignAsync(Task(seed.ElifId));
    }

    [Fact]
    public async Task Assign_OutsideTheScope_LooksLikeNotFound()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);   // Elif YM bölümünde
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Tasks.AssignAsync(Task(seed.ElifId)));

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);     // Mehmet'in danışmanı değil
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Tasks.AssignAsync(Task(seed.MehmetId)));
    }

    [Fact]
    public async Task Assign_StudentsCannotAssignTasks()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Tasks.AssignAsync(Task(seed.MehmetId)));
    }

    [Fact]
    public async Task Assign_WithPastDueDate_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using var scope = db.NewScope();

        var past = await Assert.ThrowsAsync<DomainValidationException>(() => scope.Tasks.AssignAsync(Task(seed.MehmetId, dueInDays: -1)));
        Assert.True(past.Errors.ContainsKey("DueDate"));
    }

    [Fact]
    public async Task Assign_ToAStudentWhoLeftTheProgram_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        using (var ctx = db.NewContext())
        {
            (await ctx.ProgramParticipations.SingleAsync(p => p.StudentProfileId == seed.MehmetId)).EndDate = Today.AddDays(-1);
            await ctx.SaveChangesAsync();
        }

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using var scope = db.NewScope();
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Tasks.AssignAsync(Task(seed.MehmetId)));
    }

    // ─── Değerlendirme ───────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_KeepsEveryNoteAndStatusChange_InOrder()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "İlk bölüm eksik" });

        db.Clock.AdvanceDays(2);
        using (var scope = db.NewScope())
            await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { NewStatus = StudentTaskStatus.NotCompleted });

        db.Clock.AdvanceDays(1);
        using (var scope = db.NewScope())
        {
            var final = await scope.Tasks.EvaluateAsync(task.Id,
                new EvaluateTaskRequest { Note = "Düzeltildi", NewStatus = StudentTaskStatus.Completed });

            Assert.Equal(StudentTaskStatus.Completed, final.Status);
            Assert.Equal(3, final.Evaluations.Count);
            Assert.Equal(new StudentTaskStatus?[] { null, StudentTaskStatus.NotCompleted, StudentTaskStatus.Completed },
                final.Evaluations.Select(e => e.NewStatus).ToArray());
            Assert.Equal("İlk bölüm eksik", final.Evaluations[0].Note);
        }
    }

    [Fact]
    public async Task Evaluate_WithNothingToRecord_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest()));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = " ", NewStatus = StudentTaskStatus.Open }));
    }

    [Fact]
    public async Task Evaluate_IsOnlyForTheCurrentAdvisor()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Not" }));

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Not" }));
    }

    [Fact]
    public async Task Evaluate_AfterTheAdvisorChanged_OnlyTheNewAdvisorCanEvaluate_AndOldNotesRemain()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Eski danışmanın notu" });

        using (var ctx = db.NewContext())
        {
            var open = await ctx.AdvisorAssignments.SingleAsync(a => a.StudentProfileId == seed.MehmetId && a.EndDate == null);
            open.EndDate = Today;
            ctx.AdvisorAssignments.Add(new IskurAuto.Core.Entities.AdvisorAssignment
            {
                StudentProfileId = seed.MehmetId, AdvisorUserId = seed.AdvisorYmId, StartDate = Today, AssignedByUserId = seed.HeadId
            });
            await ctx.SaveChangesAsync();
        }

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Artık yetkim yok" }));

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
        {
            var updated = await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Yeni danışmanın notu" });
            Assert.Equal(new[] { "Eski danışmanın notu", "Yeni danışmanın notu" }, updated.Evaluations.Select(e => e.Note).ToArray());
        }
    }

    // ─── Görüntüleme ve filtre ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_FollowsTheSameScopeRules()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);

        foreach (var (role, userId, dept) in new[]
        {
            (UserRole.Student, seed.MehmetUserId, (int?)null),
            (UserRole.DepartmentHead, seed.HeadId, seed.BmId),
            (UserRole.FacultyAdmin, seed.AdminId, null)
        })
        {
            db.Current.As(role, userId, dept);
            using var scope = db.NewScope();
            Assert.Equal(task.Id, (await scope.Tasks.GetByIdAsync(task.Id)).Id);
        }

        foreach (var (role, userId, dept) in new[]
        {
            (UserRole.Student, seed.ElifUserId, (int?)null),
            (UserRole.Advisor, seed.AdvisorYmId, seed.YmId)
        })
        {
            db.Current.As(role, userId, dept);
            using var scope = db.NewScope();
            await Assert.ThrowsAsync<NotFoundException>(() => scope.Tasks.GetByIdAsync(task.Id));
        }
    }

    [Fact]
    public async Task Search_FiltersByStatusAndOverdue_WithinTheRoleScope()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var soon = await AssignAsAdvisorAsync(db, seed, dueInDays: 2);
        await AssignAsAdvisorAsync(db, seed, dueInDays: 20);

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Tasks.AssignAsync(Task(seed.ElifId));

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
        {
            await scope.Tasks.EvaluateAsync(soon.Id, new EvaluateTaskRequest { NewStatus = StudentTaskStatus.Completed });
            Assert.Equal(2, (await scope.Tasks.SearchAsync(new TaskQuery())).Count);                       // Elif'inki görünmez
            Assert.Single(await scope.Tasks.SearchAsync(new TaskQuery { Status = StudentTaskStatus.Completed }));
        }

        db.Clock.AdvanceDays(10);   // 20 günlük görev hâlâ vadesinde, tamamlanan görev gecikmiş sayılmaz
        using (var scope = db.NewScope())
            Assert.Empty(await scope.Tasks.SearchAsync(new TaskQuery { OverdueOnly = true }));

        db.Clock.AdvanceDays(15);
        using (var scope = db.NewScope())
        {
            var overdue = Assert.Single(await scope.Tasks.SearchAsync(new TaskQuery { OverdueOnly = true }));
            Assert.True(overdue.IsOverdue);
        }
    }

    [Fact]
    public async Task DepartmentHead_SeesOnlyTheirDepartmentsTasks_EvenWithAnotherDepartmentFilter()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        await AssignAsAdvisorAsync(db, seed);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Tasks.AssignAsync(Task(seed.ElifId));

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using var head = db.NewScope();
        var visible = await head.Tasks.SearchAsync(new TaskQuery { DepartmentId = seed.YmId });

        Assert.Equal("Mehmet Öztürk", Assert.Single(visible).StudentName);
    }

    [Fact]
    public async Task Student_SeesOwnTasksWithAdvisorNotes_ButNoOneElses()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Devam et" });

        db.Current.As(UserRole.Student, seed.MehmetUserId);
        using (var scope = db.NewScope())
        {
            var mine = Assert.Single(await scope.Tasks.ListMineAsync(null));
            Assert.Equal(1, mine.EvaluationCount);
            Assert.Equal("Devam et", (await scope.Tasks.GetByIdAsync(task.Id)).Evaluations.Single().Note);
        }

        db.Current.As(UserRole.Student, seed.ElifUserId);
        using (var scope = db.NewScope())
            Assert.Empty(await scope.Tasks.ListMineAsync(null));
    }

    [Fact]
    public async Task Assign_AndEvaluate_WriteAuditRecordsWithoutTheNoteText()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Gizli değerlendirme metni" });

        using var verify = db.NewContext();
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.TaskAssigned));
        var log = await verify.AuditLogs.SingleAsync(a => a.Action == AuditActions.TaskEvaluated);
        Assert.DoesNotContain("Gizli", log.Details ?? "");
    }

    [Fact]
    public async Task Database_DoesNotAllowDeletingATaskThatHasEvaluations()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        var task = await AssignAsAdvisorAsync(db, seed);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.Tasks.EvaluateAsync(task.Id, new EvaluateTaskRequest { Note = "Not" });

        using var ctx = db.NewContext();
        ctx.StudentTasks.Remove(await ctx.StudentTasks.SingleAsync());
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }
}
