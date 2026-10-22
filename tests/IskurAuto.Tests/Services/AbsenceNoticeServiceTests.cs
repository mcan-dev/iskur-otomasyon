using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Service.DTOs;
using IskurAuto.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IskurAuto.Tests.Services;

public class AbsenceNoticeServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    private static SendAbsenceNoticeRequest Notice(int studentId, int daysAgo = 1) => new()
    {
        StudentId = studentId,
        Date = Today.AddDays(-daysAgo),
        Message = "Plana rağmen gelmedi, haber de vermedi."
    };

    [Fact]
    public async Task Advisor_CanNotifyTheDepartmentHeadAboutOwnStudent()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);

        using (var scope = db.NewScope())
        {
            var sent = await scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId));
            Assert.False(sent.IsRead);
            Assert.Equal("Zeynep Arslan", sent.AdvisorName);
        }

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
            Assert.Single(await scope.AbsenceNotices.SearchAsync(new AbsenceNoticeQuery { UnreadOnly = true }));
    }

    [Fact]
    public async Task Send_ForAStudentOfAnotherAdvisor_OrAFutureDay_OrTwice_IsRejected()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
            await Assert.ThrowsAsync<NotFoundException>(() => scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId)));

        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
        {
            await Assert.ThrowsAsync<DomainValidationException>(() => scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId, daysAgo: -1)));

            await scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId));
            await Assert.ThrowsAsync<ConflictException>(() => scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId)));
        }
    }

    [Theory]
    [InlineData(UserRole.DepartmentHead)]
    [InlineData(UserRole.FacultyAdmin)]
    [InlineData(UserRole.Student)]
    public async Task OnlyAdvisorsCanSend(UserRole role)
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(role, seed.HeadId, seed.BmId);
        using var scope = db.NewScope();

        await Assert.ThrowsAsync<ForbiddenException>(() => scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId)));
    }

    [Fact]
    public async Task DepartmentHead_ReadsOwnDepartmentsNotices_AndMarksThemRead()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        int noticeId;
        using (var scope = db.NewScope())
            noticeId = (await scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId))).Id;

        db.Current.As(UserRole.DepartmentHead, seed.HeadId, seed.BmId);
        using (var scope = db.NewScope())
        {
            var read = await scope.AbsenceNotices.MarkReadAsync(noticeId);
            Assert.True(read.IsRead);
            Assert.Equal("Mehmet Demir", read.ReadByName);
            Assert.Empty(await scope.AbsenceNotices.SearchAsync(new AbsenceNoticeQuery { UnreadOnly = true }));
            Assert.Single(await scope.AbsenceNotices.SearchAsync(new AbsenceNoticeQuery()));
        }
    }

    [Fact]
    public async Task OtherDepartmentsHead_CannotSeeOrReadTheNotice()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        int noticeId;
        using (var scope = db.NewScope())
            noticeId = (await scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId))).Id;

        db.Current.As(UserRole.DepartmentHead, 99, seed.YmId);
        using var scope2 = db.NewScope();
        Assert.Empty(await scope2.AbsenceNotices.SearchAsync(new AbsenceNoticeQuery { DepartmentId = seed.BmId }));
        await Assert.ThrowsAsync<NotFoundException>(() => scope2.AbsenceNotices.MarkReadAsync(noticeId));
    }

    [Fact]
    public async Task Advisor_SeesOnlyTheirOwnSentNotices()
    {
        using var db = new TestDb();
        var seed = TestData.Seed(db);
        db.Current.As(UserRole.Advisor, seed.AdvisorId, seed.BmId);
        using (var scope = db.NewScope())
            await scope.AbsenceNotices.SendAsync(Notice(seed.MehmetId));

        db.Current.As(UserRole.Advisor, seed.AdvisorYmId, seed.YmId);
        using (var scope = db.NewScope())
            Assert.Empty(await scope.AbsenceNotices.SearchAsync(new AbsenceNoticeQuery()));
    }
}
