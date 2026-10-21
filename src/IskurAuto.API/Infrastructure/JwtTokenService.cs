using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using IskurAuto.Service.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IskurAuto.API.Infrastructure;

/// <summary>appsettings.json içindeki "JwtSettings" bölümü.</summary>
public class JwtOptions
{
    public const string SectionName = "JwtSettings";
    public const string DevelopmentPlaceholderKey = "CHANGE-THIS-TO-A-STRONG-SECRET-KEY-AT-LEAST-32-CHARS";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "IskurAutoAPI";
    public string Audience { get; set; } = "IskurAutoClients";

    /// <summary>
    /// Token ömrü (dakika). Kısa tutulur çünkü rol/bölüm bilgisi token içinde taşınır;
    /// yönetici bir kullanıcıyı pasifleştirse bile eski token süresi dolana kadar geçerli kalır.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 120;
}

/// <summary>Kullanıcı için imzalı JWT erişim jetonu üretir.</summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public TokenResult CreateToken(User user)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.Role, user.Role.ToString()),
            new(AppClaimTypes.Email, user.Email)
        };

        if (user.DepartmentId.HasValue)
            claims.Add(new Claim(AppClaimTypes.DepartmentId, user.DepartmentId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
