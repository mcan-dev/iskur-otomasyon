using IskurAuto.Core.Enums;
using IskurAuto.Service.Abstractions;

namespace IskurAuto.API.Infrastructure;

/// <summary>
/// <see cref="ICurrentUser"/> uygulaması: isteği yapan kullanıcıyı JWT claim'lerinden okur.
/// Neden böyle: servis katmanı HTTP'yi bilmez. Kimlik bilgisi istek gövdesinden de gelmez;
/// yalnızca imzası doğrulanmış token'dan okunur, böylece istemci "ben şu kullanıcıyım" diyerek sahtecilik yapamaz.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private System.Security.Claims.ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId => int.TryParse(Principal?.FindFirst(AppClaimTypes.UserId)?.Value, out var id) ? id : 0;

    public UserRole? Role
    {
        get
        {
            var value = Principal?.FindFirst(AppClaimTypes.Role)?.Value;
            // IsDefined: "99" gibi sayısal ama tanımsız değerlerin rol sayılmasını engeller.
            return Enum.TryParse<UserRole>(value, ignoreCase: false, out var role) && Enum.IsDefined(role)
                ? role
                : null;
        }
    }

    public int? DepartmentId
        => int.TryParse(Principal?.FindFirst(AppClaimTypes.DepartmentId)?.Value, out var id) ? id : null;

    public string? Email => Principal?.FindFirst(AppClaimTypes.Email)?.Value;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
