using IskurAuto.Core.Entities;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;

namespace IskurAuto.Service.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. Elle yazıldı (AutoMapper yok): kod küçük, dönüşüm açıkça
/// okunur ve hangi alanın dışarı çıktığı gözle denetlenebilir (özellikle PasswordHash çıkmamalı).
/// </summary>
public static class Mappers
{
    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        Role = u.Role.ToString(),
        DepartmentId = u.DepartmentId,
        DepartmentName = u.Department?.Name,
        IsActive = u.IsActive
    };

    public static DepartmentDto ToDto(this Department d)
        => new() { Id = d.Id, Name = d.Name, Code = d.Code, IsActive = d.IsActive };

    public static WorkUnitDto ToDto(this WorkUnit w)
        => new() { Id = w.Id, Name = w.Name, Description = w.Description, IsActive = w.IsActive };

    public static AuditLogDto ToDto(this AuditLog a) => new()
    {
        Id = a.Id,
        OccurredAt = a.OccurredAt,
        UserId = a.UserId,
        UserEmail = a.UserEmail,
        Action = a.Action,
        EntityName = a.EntityName,
        EntityId = a.EntityId,
        Details = a.Details,
        IpAddress = a.IpAddress
    };

    /// <summary>Liste satırı: TC her zaman maskeli.</summary>
    public static StudentSummaryDto ToSummary(this StudentProfile s) => new()
    {
        Id = s.Id,
        FullName = s.User.FullName,
        StudentNumber = s.StudentNumber,
        MaskedNationalId = PersonalDataMasker.MaskNationalId(s.NationalId),
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department.Name,
        RegistrationYear = s.RegistrationYear,
        IsActive = s.IsActive,
        CurrentUnitName = s.CurrentUnitAssignment?.WorkUnit.Name,
        CurrentAdvisorName = s.CurrentAdvisorAssignment?.AdvisorUser.FullName
    };

    /// <param name="revealPersonalData">true ise TC ve IBAN ham; false ise maskeli döner.</param>
    public static StudentDetailDto ToDetail(this StudentProfile s, bool revealPersonalData) => new()
    {
        LeaveDayAllowance = s.LeaveDayAllowance,
        Id = s.Id,
        UserId = s.UserId,
        FullName = s.User.FullName,
        Email = s.User.Email,
        PhoneNumber = s.User.PhoneNumber,
        IsPersonalDataMasked = !revealPersonalData,
        NationalId = revealPersonalData ? s.NationalId : PersonalDataMasker.MaskNationalId(s.NationalId),
        Iban = revealPersonalData ? s.Iban : PersonalDataMasker.MaskIban(s.Iban),
        StudentNumber = s.StudentNumber,
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department.Name,
        RegistrationYear = s.RegistrationYear,
        IsActive = s.IsActive,
        CurrentUnit = s.CurrentUnitAssignment?.ToDto(),
        CurrentAdvisor = s.CurrentAdvisorAssignment?.ToDto(),
        Participations = s.Participations.OrderByDescending(p => p.StartDate).ThenByDescending(p => p.Id)
            .Select(p => new ParticipationDto
            {
                Id = p.Id, StartDate = p.StartDate, EndDate = p.EndDate, EndReason = p.EndReason, IsOpen = p.IsOpen
            }).ToList(),
        UnitHistory = s.UnitAssignments.OrderByDescending(a => a.StartDate).ThenByDescending(a => a.Id)
            .Select(a => a.ToDto()).ToList(),
        AdvisorHistory = s.AdvisorAssignments.OrderByDescending(a => a.StartDate).ThenByDescending(a => a.Id)
            .Select(a => a.ToDto()).ToList()
    };

    private static UnitAssignmentDto ToDto(this StudentUnitAssignment a) => new()
    {
        Id = a.Id,
        WorkUnitId = a.WorkUnitId,
        WorkUnitName = a.WorkUnit.Name,
        StartDate = a.StartDate,
        EndDate = a.EndDate
    };

    private static AdvisorAssignmentDto ToDto(this AdvisorAssignment a) => new()
    {
        Id = a.Id,
        AdvisorUserId = a.AdvisorUserId,
        AdvisorName = a.AdvisorUser.FullName,
        StartDate = a.StartDate,
        EndDate = a.EndDate
    };
}
