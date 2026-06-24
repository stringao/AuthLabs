using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Cookie.Controllers;

/// <summary>
/// Controller para operações de autenticação com cookies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AuthController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Login com email e senha. Cria cookie de autenticação.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        return Ok(new { message = "Login realizado com sucesso" });
    }

    /// <summary>
    /// Logout - remove o cookie de autenticação.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    /// <summary>
    /// Retorna informações do usuário autenticado atual.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _signInManager.UserManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { message = "Usuário não encontrado" });
        }

        var roles = await _signInManager.UserManager.GetRolesAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            userName = user.UserName,
            roles = roles
        });
    }

    /// <summary>
    /// Access denied response.
    /// </summary>
    [HttpGet("access-denied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return Forbid();
    }
}

public record LoginRequest(string Email, string Password);
