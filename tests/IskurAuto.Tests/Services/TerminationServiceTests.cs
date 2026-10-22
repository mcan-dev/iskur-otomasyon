using IskurAuto.Core.Constants;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class TerminationServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    private static RecordTerminationRequest Decision(int studentId, TerminationBasis basis = TerminationBasis.AbsenceLimitExceeded) => new()
    {
        StudentId = studentId,
        Basis = basis,
        Note = "Mazeretsiz devamsızlık sınırı aşıldı, inceleme tamamlandı."
    };

    private static async Task ApproveLeaveAsync(TestDb db, SeedResult seed, DateOnly from, DateOnly to)
    {
        db.Current.As(UserRole.Student, seed.MehmetUserId);
        LeaveResultDto requested;
        using (var scope = db.NewScope())
            requested = await scope.Leaves.RequestAsync(new CreateLeaveRequest { Type = LeaveType.Excuse, StartDate = from, EndDate = to, Reason = "Aile işi" });

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Leaves.ReviewAsync(requested.Leave.Id, new ReviewLeaveRequest { Approve = true });
    }

    [Fact]
    public async Task Record_EndsTheParticipation_AndKeepsASnapshotOfTheBalance()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        await ApproveLeaveAsync(db, seed, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 13));   // 10 gün

        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        TerminationDto dto;
        using (var scope = db.NewScope())
            dto = await scope.Terminations.RecordAsync(Decision(seed.MehmetId));

        Assert.Equal("Mehmet Öztürk", dto.StudentName);
        Assert.Equal("Ayşe Kaya", dto.DecidedByName);
        Assert.Equal(Today, dto.DecisionDate);
        Assert.Equal(10m, dto.LeaveDaysUsed);

        using var verify = db.NewContext();
        var participation = await verify.ProgramParticipations.SingleAsync(p => p.StudentProfileId == seed.MehmetId);
        Assert.Equal(Today, participation.EndDate);
        Assert.Contains("İlişik kesme", participation.EndReason);
        Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == AuditActions.TerminationRecorded));
    }

    [Fact]
    public async Task Record_ForAnAlreadyPassiveStudent_IsRejected_AndLeavesNoDecision()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);

        using (var scope = db.NewScope())
            await scope.Terminations.RecordAsync(Decision(seed.MehmetId));
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ConflictException>(() => scope.Terminations.RecordAsync(Decision(seed.MehmetId)));

        using var verify = db.NewContext();
        Assert.Equal(1, await verify.TerminationDecisions.CountAsync());
    }

    [Fact]
    public async Task Record_NeedsARealReason_AndASensibleDate()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using var scope = db.NewScope();

        var shortNote = Decision(seed.MehmetId);
        shortNote.Note = "kısa";
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Terminations.RecordAsync(shortNote));

        var future = Decision(seed.MehmetId);
        future.DecisionDate = Today.AddDays(1);
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Terminations.RecordAsync(future));

        var beforeStart = Decision(seed.MehmetId);
        beforeStart.DecisionDate = Today.AddDays(-60);
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Terminations.RecordAsync(beforeStart));

        var unknown = Decision(seed.MehmetId);
        unknown.Basis = (TerminationBasis)99;
        await Assert.ThrowsAsync<DomainValidationException>(() => scope.Terminations.RecordAsync(unknown));

        using var verify = db.NewContext();
        Assert.False(await verify.TerminationDecisions.AnyAsync());
        Assert.Null((await verify.ProgramParticipations.SingleAsync(p => p.StudentProfileId == seed.MehmetId)).EndDate);
    }

    [Theory]
    [InlineData(UserRole.DepartmentHead)]
    [InlineData(UserRole.Advisor)]
    [InlineData(UserRole.Student)]
    public async Task OnlyTheFacultyOfficeRecordsDecisions(UserRole role)
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(role, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.Terminations.RecordAsync(Decision(seed.MehmetId)));
    }

    [Fact]
    public async Task Search_IsScopedForDepartmentHeads()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
        {
            await scope.Terminations.RecordAsync(Decision(seed.MehmetId));
            await scope.Terminations.RecordAsync(Decision(seed.ElifId, TerminationBasis.StudentRequest));
            Assert.Equal(2, (await scope.Terminations.SearchAsync(new TerminationQuery())).Count);
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
        {
            var visible = await scope.Terminations.SearchAsync(new TerminationQuery { DepartmentId = seed.YmId });
            Assert.Equal("Mehmet Öztürk", Assert.Single(visible).StudentName);
        }

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<ForbiddenException>(() => scope.Terminations.SearchAsync(new TerminationQuery()));
    }

    [Fact]
    public async Task AfterTheDecision_TheStudentAppearsAsPassive_AndKeepsAllHistory()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.FacultyAdmin, seed.AdminId);
        using (var scope = db.NewScope())
            await scope.Terminations.RecordAsync(Decision(seed.MehmetId));

        using var verify = db.NewContext();
        var student = await verify.StudentProfiles
            .Include(s => s.Participations).Include(s => s.AdvisorAssignments).Include(s => s.UnitAssignments)
            .SingleAsync(s => s.Id == seed.MehmetId);

        Assert.False(student.IsActive);
        Assert.Single(student.Participations);
        Assert.NotNull(student.AdvisorAssignments.Single().EndDate);   // kapanır, silinmez
        Assert.NotNull(student.UnitAssignments.Single().EndDate);
    }
}
