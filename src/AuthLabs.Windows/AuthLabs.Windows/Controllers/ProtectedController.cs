using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Windows.Controllers;

/// <summary>
/// Controller com endpoints protegidos por autenticacao Windows.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = "Negotiate")]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido que requer autenticacao Windows.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var user = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;
        return Ok(new
        {
            user,
            authType,
            message = "Autenticado via Windows"
        });
    }

    /// <summary>
    /// Area administrativa - apenas para usuarios com role Admin.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmin()
    {
        return Ok(new
        {
            message = "Area administrativa",
            user = User.Identity?.Name
        });
    }

    /// <summary>
    /// Area publica - qualquer usuario autenticado.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult GetPublic()
    {
        return Ok(new
        {
            message = "Area publica - sem autenticacao"
        });
    }
}
