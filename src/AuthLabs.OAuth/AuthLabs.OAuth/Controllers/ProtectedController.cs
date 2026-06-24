using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.OAuth.Controllers;

/// <summary>
/// Controller com endpoints protegidos por autenticação.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint público - não requer autenticação.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public()
    {
        return Ok(new { message = "Este endpoint é público", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Endpoint protegido - requer autenticação.
    /// </summary>
    [HttpGet("secure")]
    public IActionResult Secure()
    {
        return Ok(new
        {
            message = "Este endpoint requer autenticação",
            user = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated
        });
    }

    /// <summary>
    /// Endpoint que requer claim específica.
    /// </summary>
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();
        return Ok(new
        {
            message = "Perfil do usuário",
            user = User.Identity?.Name,
            email = User.FindFirst("email")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            provider = User.FindFirst(System.Security.Claims.ClaimTypes.AuthenticationMethod)?.Value,
            claims
        });
    }

    /// <summary>
    /// Retorna os tokens externos (para debugging).
    /// </summary>
    [HttpGet("tokens")]
    public async Task<IActionResult> GetTokens()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync();
        if (!authenticateResult.Succeeded)
        {
            return Unauthorized(new { message = "Não autenticado" });
        }

        var tokens = new Dictionary<string, string?>();
        if (authenticateResult.Properties?.Items.ContainsKey(".Token.access_token") == true)
        {
            tokens["access_token"] = authenticateResult.Properties.Items[".Token.access_token"];
        }
        if (authenticateResult.Properties?.Items.ContainsKey(".Token.refresh_token") == true)
        {
            tokens["refresh_token"] = authenticateResult.Properties.Items[".Token.refresh_token"];
        }

        return Ok(new { tokens });
    }
}