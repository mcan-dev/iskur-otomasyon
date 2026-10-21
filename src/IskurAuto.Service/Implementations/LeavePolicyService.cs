using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;

namespace IskurAuto.Service.Implementations;

public class LeavePolicyService : ILeavePolicyProvider, ILeaveSettingsService
{
    private const string Prefix = "Leave.";
    private const string LeaveLimit = Prefix + "DayLimit";
    private const string LeaveWarning = Prefix + "WarningRemainingDays";
    private const string AbsenceLimit = Prefix + "AbsenceDayLimit";
    private const string AbsenceWarning = Prefix + "AbsenceWarningRemainingDays";
    private const string HealthCounts = Prefix + "HealthCountsTowardLimit";

    private readonly ISystemSettingRepository _settings;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public LeavePolicyService(ISystemSettingRepository settings, IUnitOfWork uow, IAuditService audit, ICurrentUser current)
    {
        _settings = settings;
        _uow = uow;
        _audit = audit;
        _current = current;
    }

    async Task<LeavePolicy> ILeavePolicyProvider.GetAsync(CancellationToken ct) => await LoadAsync(ct);

    async Task<LeavePolicyDto> ILeaveSettingsService.GetAsync(CancellationToken ct)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);
        return ToDto(await LoadAsync(ct));
    }

    public async Task<LeavePolicyDto> UpdateAsync(LeavePolicyDto request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var stored = (await _settings.GetByPrefixAsync(Prefix, ct)).ToDictionary(s => s.Key);

        await Upsert(stored, LeaveLimit, request.LeaveDayLimit.ToString());
        await Upsert(stored, LeaveWarning, request.LeaveWarningRemainingDays.ToString());
        await Upsert(stored, AbsenceLimit, request.AbsenceDayLimit.ToString());
        await Upsert(stored, AbsenceWarning, request.AbsenceWarningRemainingDays.ToString());
        await Upsert(stored, HealthCounts, request.HealthLeaveCountsTowardLimit.ToString());

        await _audit.RecordAsync(AuditActions.LeavePolicyChanged, nameof(SystemSetting), ct: ct);
        await _uow.SaveChangesAsync(ct);

        return request;
    }

    private async Task Upsert(Dictionary<string, SystemSetting> stored, string key, string value)
    {
        if (stored.TryGetValue(key, out var existing))
            existing.Value = value;
        else
            await _settings.AddAsync(new SystemSetting { Key = key, Value = value });
    }

    private async Task<LeavePolicy> LoadAsync(CancellationToken ct)
    {
        var values = (await _settings.GetByPrefixAsync(Prefix, ct)).ToDictionary(s => s.Key, s => s.Value);
        var policy = new LeavePolicy();

        policy.LeaveDayLimit = Int(values, LeaveLimit, policy.LeaveDayLimit);
        policy.LeaveWarningRemainingDays = Int(values, LeaveWarning, policy.LeaveWarningRemainingDays);
        policy.AbsenceDayLimit = Int(values, AbsenceLimit, policy.AbsenceDayLimit);
        policy.AbsenceWarningRemainingDays = Int(values, AbsenceWarning, policy.AbsenceWarningRemainingDays);
        policy.HealthLeaveCountsTowardLimit = values.TryGetValue(HealthCounts, out var flag)
            ? bool.TryParse(flag, out var parsed) && parsed
            : policy.HealthLeaveCountsTowardLimit;

        return policy;
    }

    private static int Int(Dictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed) ? parsed : fallback;

    private static LeavePolicyDto ToDto(LeavePolicy p) => new()
    {
        LeaveDayLimit = p.LeaveDayLimit,
        LeaveWarningRemainingDays = p.LeaveWarningRemainingDays,
        AbsenceDayLimit = p.AbsenceDayLimit,
        AbsenceWarningRemainingDays = p.AbsenceWarningRemainingDays,
        HealthLeaveCountsTowardLimit = p.HealthLeaveCountsTowardLimit
    };
}
