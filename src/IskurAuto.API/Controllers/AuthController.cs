using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IskurAuto.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace IskurAuto.API.Controllers;

/// <summary>
/// TEST AMAÇLI kimlik doğrulama controller'ı.
/// Gerçek veritabanı doğrulaması yapmaz; yalnızca Swagger üzerinden
/// JWT token almak için kullanılır.
/// ⚠ Production'da bu controller kaldırılmalı veya gerçek auth ile değiştirilmelidir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// [TEST] Kullanıcı adına göre JWT token üretir.
    /// Geçerli değerler: "maker" (FacultySecretary) veya "checker" (SksAdmin).
    /// </summary>
    /// <param name="request">Giriş bilgileri. Username: "maker" veya "checker".</param>
    /// <response code="200">JWT token başarıyla üretildi.</response>
    /// <response code="400">Geçersiz veya eksik kullanıcı adı.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new ProblemDetails
            {
                Title  = "Geçersiz İstek",
                Detail = "Username alanı boş olamaz.",
                Status = StatusCodes.Status400BadRequest
            });

        var claims = request.Username.ToLowerInvariant() switch
        {
            "maker" => new List<Claim>
            {
                new(AppClaimTypes.UserId,    "1"),
                new(AppClaimTypes.Role,      "FacultySecretary"),
                new(AppClaimTypes.FacultyId, "1"),
                new(JwtRegisteredClaimNames.Sub, "maker"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            "checker" => new List<Claim>
            {
                new(AppClaimTypes.UserId,    "2"),
                new(AppClaimTypes.Role,      "SksAdmin"),
                new(JwtRegisteredClaimNames.Sub, "checker"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            _ => null
        };

        if (claims is null)
            return BadRequest(new ProblemDetails
            {
                Title  = "Geçersiz Kullanıcı",
                Detail = $"'{request.Username}' tanımlı değil. Geçerli değerler: 'maker', 'checker'.",
                Status = StatusCodes.Status400BadRequest
            });

        var jwtSection = _configuration.GetSection("JwtSettings");
        var secretKey  = jwtSection["SecretKey"]!;
        var issuer     = jwtSection["Issuer"]!;
        var audience   = jwtSection["Audience"]!;
        var expiryMins = int.TryParse(jwtSection["ExpirationMinutes"], out var m) ? m : 480;

        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(expiryMins),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponseDto
        {
            Token     = tokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMins),
            Username  = request.Username,
            Role      = claims.First(c => c.Type == AppClaimTypes.Role).Value
        });
    }

    /// <summary>
    /// [TEST] Token içindeki Claim'leri döndürür. (Hata ayıklama için)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMyClaims()
    {
        return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
    }
}

// ─── DTO'lar (plain class — Swashbuckle uyumlu) ───────────────────────────────

/// <summary>Test girişi için istek modeli.</summary>
public class LoginRequestDto
{
    /// <summary>Kullanıcı adı. Geçerli değerler: "maker" veya "checker".</summary>
    /// <example>maker</example>
    public string Username { get; set; } = string.Empty;
}

/// <summary>Başarılı giriş yanıtı — JWT token ve meta bilgiler.</summary>
public class LoginResponseDto
{
    /// <summary>Swagger Authorize butonuna yapıştırılacak JWT token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Token'ın geçerlilik bitiş zamanı (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Giriş yapan kullanıcı adı.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Kullanıcının atanan rolü.</summary>
    public string Role { get; set; } = string.Empty;
}
