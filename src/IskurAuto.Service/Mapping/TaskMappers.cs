using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Service.DTOs;

namespace IskurAuto.Service.Mapping;

public static class TaskMappers
{
    public static TaskDto ToDto(this StudentTask t, DateOnly today, bool withEvaluations = false) => new()
    {
        Id = t.Id,
        StudentId = t.StudentProfileId,
        StudentName = t.StudentProfile.User.FullName,
        DepartmentName = t.StudentProfile.Department.Name,
        Title = t.Title,
        Description = t.Description,
        AssignedDate = t.AssignedDate,
        DueDate = t.DueDate,
        AssignedByName = t.AssignedByUser.FullName,
        Status = t.Status,
        StatusChangedAt = t.StatusChangedAt,
        IsOverdue = t.Status == StudentTaskStatus.Open && t.DueDate < today,
        EvaluationCount = t.Evaluations.Count,
        Evaluations = withEvaluations
            ? t.Evaluations.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).Select(e => e.ToDto()).ToList()
            : Array.Empty<TaskEvaluationDto>()
    };

    public static TaskEvaluationDto ToDto(this TaskEvaluation e) => new()
    {
        Id = e.Id,
        AuthorName = e.AuthorUser.FullName,
        Note = e.Note,
        NewStatus = e.NewStatus,
        CreatedAt = e.CreatedAt
    };

    public static AbsenceNoticeDto ToDto(this AbsenceNotice n) => new()
    {
        Id = n.Id,
        StudentId = n.StudentProfileId,
        StudentName = n.StudentProfile.User.FullName,
        DepartmentName = n.StudentProfile.Department.Name,
        Date = n.Date,
        Message = n.Message,
        AdvisorName = n.AdvisorUser.FullName,
        CreatedAt = n.CreatedAt,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        ReadByName = n.ReadByUser?.FullName
    };
}
