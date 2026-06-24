using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller protegido - acessível a qualquer usuário autenticado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido - requer autenticação.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var userName = User.Identity?.Name;
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        return Ok(new
        {
            message = "Recurso protegido",
            user = userName,
            roles
        });
    }
}
