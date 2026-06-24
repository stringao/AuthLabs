using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller administrativo - acessível apenas para Admin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    /// <summary>
    /// Lista todos os usuários - apenas Admin.
    /// </summary>
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        return Ok(new { message = "Lista de usuários - área administrativa" });
    }

    /// <summary>
    /// Dashboard administrativo - apenas Admin.
    /// </summary>
    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok(new { message = "Dashboard administrativo" });
    }
}
