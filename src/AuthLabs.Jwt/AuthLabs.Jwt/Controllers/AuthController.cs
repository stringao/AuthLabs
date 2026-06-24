using AuthLabs.Jwt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Jwt.Controllers;

/// <summary>
/// Controller para operações de autenticação (login, refresh, logout).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Login com email e senha. Retorna access token e refresh token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        if (result == null)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        return Ok(new
        {
            accessToken = result.Value.accessToken,
            refreshToken = result.Value.refreshToken,
            expiresIn = 900 // 15 minutes in seconds
        });
    }

    /// <summary>
    /// Refresh do access token usando o refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (result == null)
        {
            return Unauthorized(new { message = "Refresh token inválido ou expirado" });
        }

        return Ok(new
        {
            accessToken = result.Value.accessToken,
            refreshToken = result.Value.refreshToken,
            expiresIn = 900
        });
    }

    /// <summary>
    /// Logout - revoga o refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok(new { message = "Logout realizado com sucesso" });
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);