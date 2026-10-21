using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IskurAuto.API.Controllers;

/// <summary>Giriş, öğrenci ilk kaydı ve hesap işlemleri.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    // Controller bilerek "ince": girdiyi alır, servise verir, sonucu HTTP'ye çevirir. İş kuralı yok.
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>E-posta ve parola ile giriş yapar, JWT erişim jetonu döner.</summary>
    /// <response code="401">E-posta veya parola hatalı ya da hesap pasif.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>
    /// Öğrenci ilk kayıt formu (belge madde 3). Hesap, profil ve ilk program katılım dönemi birlikte oluşur.
    /// </summary>
    /// <response code="400">Alan doğrulama hataları (TC, IBAN, telefon, parola...).</response>
    /// <response code="409">E-posta, T.C. kimlik no veya öğrenci numarası zaten kayıtlı.</response>
    [HttpPost("register-student")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistrationResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResultDto>> RegisterStudent(
        [FromBody] StudentRegistrationRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterStudentAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Giriş yapmış kullanıcının kendi hesap bilgileri.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
        => Ok(await _auth.GetCurrentUserAsync(ct));

    /// <summary>Kendi parolasını değiştirir.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(request, ct);
        return NoContent();
    }
}
