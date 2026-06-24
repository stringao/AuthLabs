using AuthLabs.Rbac.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller para operações de autenticação (login, logout).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly SignInManager<Shared.Models.User> _signInManager;

    public AuthController(IRoleService roleService, SignInManager<Shared.Models.User> signInManager)
    {
        _roleService = roleService;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Login com email e senha usando cookie authentication.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _roleService.GetUserByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        var roles = await _roleService.GetUserRolesAsync(user);
        return Ok(new
        {
            message = "Login realizado com sucesso",
            user = user.Email,
            roles
        });
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
    /// Retorna informações do usuário atual.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var email = User.Identity?.Name;
        if (email == null)
        {
            return Unauthorized(new { message = "Não autenticado" });
        }

        var user = await _roleService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new { message = "Usuário não encontrado" });
        }

        var roles = await _roleService.GetUserRolesAsync(user);
        return Ok(new
        {
            user = user.Email,
            roles
        });
    }
}

public record LoginRequest(string Email, string Password);
