using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Jwt.Controllers;

/// <summary>
/// Controller de exemplo com endpoints protegidos por JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido - requer token JWT válido.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var userName = User.Identity?.Name;
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new
        {
            message = "Você está autenticado!",
            user = userName,
            claims
        });
    }

    /// <summary>
    /// Endpoint que requer claim específica.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminOnly()
    {
        return Ok(new { message = "Área administrativa" });
    }
}