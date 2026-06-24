using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Cookie.Controllers;

/// <summary>
/// Controller para endpoints protegidos por autenticação e autorização.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Retorna informações do usuário autenticado e seus claims.
    /// </summary>
    [HttpGet]
    public IActionResult GetUserInfo()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var identity = User.Identity;

        return Ok(new
        {
            isAuthenticated = identity?.IsAuthenticated ?? false,
            name = identity?.Name,
            authenticationType = identity?.AuthenticationType,
            claims
        });
    }

    /// <summary>
    /// Endpoint protegido apenas para usuários com role Admin.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminInfo()
    {
        return Ok(new
        {
            message = "Acesso admin concedido",
            user = User.Identity?.Name,
            role = "Admin"
        });
    }

    /// <summary>
    /// Endpoint protegido apenas para usuários com role Manager.
    /// </summary>
    [HttpGet("manager")]
    [Authorize(Roles = "Manager")]
    public IActionResult GetManagerInfo()
    {
        return Ok(new
        {
            message = "Acesso manager concedido",
            user = User.Identity?.Name,
            role = "Manager"
        });
    }

    /// <summary>
    /// Endpoint protegido para usuários autenticados (qualquer role).
    /// </summary>
    [HttpGet("authenticated")]
    public IActionResult GetAuthenticatedInfo()
    {
        return Ok(new
        {
            message = "Acesso autenticado concedido",
            user = User.Identity?.Name
        });
    }
}
