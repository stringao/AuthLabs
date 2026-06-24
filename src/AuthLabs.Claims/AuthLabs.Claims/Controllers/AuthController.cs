using System.Security.Claims;
using AuthLabs.Claims.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Claims.Controllers;

/// <summary>
/// Controller de autenticação com suporte a claims.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IClaimsService _claimsService;

    public AuthController(IClaimsService claimsService)
    {
        _claimsService = claimsService;
    }

    /// <summary>
    /// Login de usuário e geração de claims.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Validação simples de credenciais
        var validUsers = new Dictionary<string, (string Password, Dictionary<string, string> Claims)>
        {
            ["admin@authlabs.com"] = ("Admin123!", new Dictionary<string, string>
            {
                ["Document:Edit"] = "true",
                ["Document:Delete"] = "true",
                ["User:Manage"] = "true",
                ["Subscription:Tier"] = "Premium"
            }),
            ["manager@authlabs.com"] = ("Manager123!", new Dictionary<string, string>
            {
                ["Document:Edit"] = "true",
                ["Subscription:Tier"] = "Standard"
            }),
            ["user@authlabs.com"] = ("User123!", new Dictionary<string, string>
            {
                ["Document:Edit"] = "true",
                ["Subscription:Tier"] = "Basic"
            }),
            ["guest@authlabs.com"] = ("Guest123!", new Dictionary<string, string>())
        };

        if (!validUsers.TryGetValue(request.Email, out var userData) || userData.Password != request.Password)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        // Cria identity com claims
        var claims = new List<Claim> { new(ClaimTypes.Email, request.Email) };
        foreach (var (key, value) in userData.Claims)
        {
            claims.Add(new Claim(key, value));
        }

        return Ok(new
        {
            email = request.Email,
            claims = userData.Claims
        });
    }
}

/// <summary>
/// Request de login.
/// </summary>
public record LoginRequest(string Email, string Password);
