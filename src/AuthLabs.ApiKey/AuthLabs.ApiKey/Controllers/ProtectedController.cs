using AuthLabs.ApiKey.Models;
using AuthLabs.ApiKey.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.ApiKey.Controllers;

/// <summary>
/// Controller protegido por API Key - demonstra autenticacao e authorization por scope.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProtectedController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ProtectedController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// Endpoint protegido - retorna info do cliente autenticado.
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult GetInfo()
    {
        var clientName = User.Identity?.Name;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "Acesso autorizado",
            clientName,
            role,
            authenticationType = User.Identity?.AuthenticationType
        });
    }

    /// <summary>
    /// Endpoint que requer scope 'read'.
    /// </summary>
    [HttpGet("read")]
    [Authorize(Roles = "User,Admin")]
    public IActionResult Read()
    {
        var clientName = User.Identity?.Name;
        var hasReadScope = User.HasClaim("scope", "read");

        if (!hasReadScope)
        {
            return Forbid("Scope 'read' requerido");
        }

        return Ok(new
        {
            message = "Leitura autorizada",
            clientName,
            scope = "read"
        });
    }

    /// <summary>
    /// Endpoint que requer scope 'write'.
    /// </summary>
    [HttpGet("write")]
    [Authorize(Roles = "User,Admin")]
    public IActionResult Write()
    {
        var clientName = User.Identity?.Name;
        var hasWriteScope = User.HasClaim("scope", "write");

        if (!hasWriteScope)
        {
            return Forbid("Scope 'write' requerido");
        }

        return Ok(new
        {
            message = "Escrita autorizada",
            clientName,
            scope = "write"
        });
    }

    /// <summary>
    /// Endpoint que requer scope 'delete' (apenas Admin).
    /// </summary>
    [HttpGet("delete")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete()
    {
        var clientName = User.Identity?.Name;
        var hasDeleteScope = User.HasClaim("scope", "delete");

        if (!hasDeleteScope)
        {
            return Forbid("Scope 'delete' requerido");
        }

        return Ok(new
        {
            message = "Delete autorizado",
            clientName,
            scope = "delete"
        });
    }
}
