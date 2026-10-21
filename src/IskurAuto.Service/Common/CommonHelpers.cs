using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;

namespace IskurAuto.Service.Common;

/// <summary>Gerçek saat. Türkiye 2016'dan beri yıl boyu UTC+3 kullandığı için sabit ofset yeterlidir.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.AddHours(3));
}

/// <summary>
/// Kişisel verileri listelerde ve yetkisiz rollere gösterirken maskeler (KVKK veri minimizasyonu).
/// Ham değer yalnızca gerçekten ihtiyacı olana (öğrencinin kendisi, fakülte yönetimi) gösterilir.
/// </summary>
public static class PersonalDataMasker
{
    /// <summary>"12345678950" → "123******50"</summary>
    public static string MaskNationalId(string value)
        => value.Length < 6 ? new string('*', value.Length) : value[..3] + new string('*', value.Length - 5) + value[^2..];

    /// <summary>"TR400006200000000000000001" → "TR40********************01"</summary>
    public static string MaskIban(string value)
        => value.Length < 8 ? new string('*', value.Length) : value[..4] + new string('*', value.Length - 6) + value[^2..];
}

/// <summary>Rol kontrolünü her serviste tekrar yazmamak için ortak yardımcı.</summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// İsteği yapan kişi izin verilen rollerden biri değilse <see cref="ForbiddenException"/> fırlatır.
    /// Neden serviste de kontrol var: API'deki [Authorize] ilk savunmadır; servis başka bir yerden
    /// (örn. zamanlanmış iş) çağrılsa bile kuralın delinmemesi için ikinci savunma burada durur.
    /// </summary>
    public static void RequireRole(this ICurrentUser user, params UserRole[] allowed)
    {
        if (!user.IsAuthenticated || user.Role is null)
            throw new AuthenticationFailedException("Bu işlem için oturum açmanız gerekiyor.");

        if (!allowed.Contains(user.Role.Value))
            throw new ForbiddenException();
    }
}

/// <summary>Birden çok alan hatasını toplayıp tek seferde bildirir; form kullanıcısı hepsini birden görür.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public void Add(string field, string? message)
    {
        if (message is null) return;
        if (!_errors.TryGetValue(field, out var list)) _errors[field] = list = new List<string>();
        list.Add(message);
    }

    public bool Any => _errors.Count > 0;

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
            throw new DomainValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}
