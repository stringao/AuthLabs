using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthPolicies = AuthLabs.Claims.Authorization.AuthorizationPolicies;

namespace AuthLabs.Claims.Controllers;

/// <summary>
/// Controller protegido com políticas de claims.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Retorna informações do usuário logado.
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult GetUserInfo()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(new
        {
            email,
            claims
        });
    }

    /// <summary>
    /// Endpoint para editar documentos. Requer claim Document:Edit=true.
    /// </summary>
    [HttpGet("edit")]
    [Authorize(Policy = AuthPolicies.CanEditDocuments)]
    public IActionResult Edit()
    {
        return Ok(new { message = "Acesso permitido: você pode editar documentos" });
    }

    /// <summary>
    /// Endpoint para excluir documentos. Requer claim Document:Delete=true.
    /// </summary>
    [HttpGet("delete")]
    [Authorize(Policy = AuthPolicies.CanDeleteDocuments)]
    public IActionResult Delete()
    {
        return Ok(new { message = "Acesso permitido: você pode excluir documentos" });
    }

    /// <summary>
    /// Endpoint administrativo. Requer claim User:Manage=true.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = AuthPolicies.CanManageUsers)]
    public IActionResult Admin()
    {
        return Ok(new { message = "Acesso permitido: você pode gerenciar usuários" });
    }

    /// <summary>
    /// Endpoint premium. Requer claim Subscription:Tier=Premium.
    /// </summary>
    [HttpGet("premium")]
    [Authorize(Policy = AuthPolicies.IsPremiumUser)]
    public IActionResult Premium()
    {
        return Ok(new { message = "Acesso permitido: você é um usuário Premium" });
    }
}
