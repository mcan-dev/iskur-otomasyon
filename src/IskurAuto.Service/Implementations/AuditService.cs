using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;

namespace IskurAuto.Service.Implementations;

/// <summary>
/// Denetim izi. "Kim, ne zaman, neye ne yaptı" sorusunun cevabı burada tutulur (belge madde 9 ve 11).
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _logs;
    private readonly IClock _clock;
    private readonly ICurrentUser _current;

    public AuditService(IAuditLogRepository logs, IClock clock, ICurrentUser current)
    {
        _logs = logs;
        _clock = clock;
        _current = current;
    }

    public Task RecordAsync(string action, string entityName, int? entityId = null, string? details = null,
        int? actorUserId = null, string? actorEmail = null, CancellationToken ct = default)
    {
        // Açıkça verilen aktör (örn. giriş denemesi) yoksa isteği yapan kullanıcı kullanılır.
        var userId = actorUserId ?? (_current.IsAuthenticated ? _current.UserId : null);
        var email = actorEmail ?? (_current.IsAuthenticated ? _current.Email : null);

        return _logs.AddAsync(new AuditLog
        {
            OccurredAt = _clock.UtcNow,
            UserId = userId,
            UserEmail = email,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = _current.IpAddress
        }, ct);
    }

    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var criteria = new AuditLogSearchCriteria
        {
            UserId = query.UserId,
            Action = query.Action,
            EntityName = query.EntityName,
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc
        };

        var page = await _logs.SearchAsync(criteria,
            new PageRequest { Page = query.Page, PageSize = query.PageSize }, ct);

        return page.Map(a => a.ToDto());
    }
}
