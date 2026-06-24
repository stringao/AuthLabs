using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthLabs.Windows.Services;

namespace AuthLabs.Windows.Controllers;

/// <summary>
/// Controller para endpoints de autenticacao Windows.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IWindowsAuthService _windowsAuthService;

    public AuthController(IWindowsAuthService windowsAuthService)
    {
        _windowsAuthService = windowsAuthService;
    }

    /// <summary>
    /// Obtem informacoes do usuario Windows atual.
    /// Requer autenticacao Windows.
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Negotiate")]
    public IActionResult GetCurrentUser()
    {
        var userName = _windowsAuthService.GetCurrentUserName(User);
        var authType = _windowsAuthService.GetAuthenticationType(User);
        var isAdmin = User.IsInRole("Admin");

        return Ok(new
        {
            userName,
            authType,
            isAdmin,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
            message = "Usuario Windows autenticado"
        });
    }

    /// <summary>
    /// Retorna informacoes sobre a autenticacao Windows disponivel.
    /// </summary>
    [HttpGet("windows-login")]
    [AllowAnonymous]
    public IActionResult GetWindowsLoginInfo()
    {
        return Ok(new
        {
            authenticationType = "Negotiate",
            schemes = new[] { "Kerberos", "NTLM" },
            description = "Windows Authentication usa Kerberos ou NTLM para autenticacao integrada com Active Directory",
            note = "Requer ambiente Windows (IIS ou Kestrel com Negotiate)"
        });
    }

    /// <summary>
    /// Obtem grupos do AD do usuario atual.
    /// </summary>
    [HttpGet("ad-groups")]
    [Authorize(AuthenticationSchemes = "Negotiate")]
    public async Task<IActionResult> GetUserAdGroups()
    {
        var groups = await _windowsAuthService.GetUserAdGroupsAsync(User);
        return Ok(new
        {
            userName = _windowsAuthService.GetCurrentUserName(User),
            adGroups = groups
        });
    }
}
