using System.Security.Claims;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.OAuth.Controllers;

/// <summary>
/// Controller para autenticação OAuth 2.0 + OpenID Connect.
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
    /// Inicia o fluxo de login OAuth com o provider especificado.
    /// </summary>
    /// <param name="provider">Nome do provider (Google, GitHub)</param>
    /// <param name="returnUrl">URL para redirecionamento após login</param>
    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider, [FromQuery] string returnUrl = "/")
    {
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, returnUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Callback do provider OAuth após autenticação bem-sucedida.
    /// </summary>
    /// <param name="provider">Nome do provider</param>
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Unauthorized(new { message = "Falha ao obter informações externas" });
        }

        // Tentar login com o provider externo
        var result = await _signInManager.ExternalLoginSignInAsync(
            provider.ToLowerInvariant(),
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            return Ok(new { message = $"Login via {provider} bem-sucedido", provider });
        }

        // Se não conseguiu fazer login, verificar se usuário já existe
        var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value
            ?? info.Principal.FindFirst("email")?.Value
            ?? info.Principal.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(email))
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Vincular login externo ao usuário existente
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.ExternalLoginSignInAsync(
                    provider.ToLowerInvariant(),
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);
                return Ok(new { message = $"Login via {provider} bem-sucedido (usuário existente)", provider });
            }

            // Criar novo usuário
            var newUser = new User
            {
                Email = email,
                UserName = email.Split('@')[0],
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.Split('@')[0].ToUpperInvariant()
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(newUser, info);
                await _userManager.AddToRoleAsync(newUser, "User");
                await _signInManager.ExternalLoginSignInAsync(
                    provider.ToLowerInvariant(),
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);
                return Ok(new { message = $"Usuário registrado via {provider}", provider });
            }
        }

        return BadRequest(new { message = "Falha ao processar autenticação externa", isLockedOut = result.IsLockedOut, isNotAllowed = result.IsNotAllowed });
    }

    /// <summary>
    /// Realiza logout do usuário atual.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    /// <summary>
    /// Retorna informações do usuário autenticado.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { message = "Usuário não autenticado" });
        }

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var provider = User.FindFirst(ClaimTypes.AuthenticationMethod)?.Value;

        return Ok(new
        {
            email,
            name,
            provider,
            isAuthenticated = true
        });
    }
}