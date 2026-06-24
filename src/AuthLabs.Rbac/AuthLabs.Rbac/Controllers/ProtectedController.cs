/*
 * JUNIOR: ProtectedController - Exemplo de Autorização Simples
 * ===========================================================
 *
 * Este controller demonstra o uso mais básico de [Authorize]
 * Qualquer usuário logado pode acessar - não importa a role.
 *
 * CONCEITOS IMPORTANTES:
 *
 * 1. User.Claims - é uma coleção de TODAS as claims do usuário
 *    Claims são pares chave-valor como:
 *    - ClaimTypes.NameIdentifier = "1" (ID do usuário)
 *    - ClaimTypes.Email = "user@email.com"
 *    - ClaimTypes.Role = "Admin" (ou Manager, User, Guest)
 *
 * 2. Roles também são Claims!
 *    O Identity automaticamente converte cada Role em uma Claim
 *    Type = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
 *    Value = nome da role ("Admin", "Manager", etc.)
 *
 * 3. Diferença entre RBAC e Claims:
 *    - RBAC: authorization baseada em roles (Admin ou não Admin)
 *    - Claims: authorization baseada em propriedades específicas
 *      (ex: "pode acessar relatórios", "trabalha no departamento X")
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller protegido - acessível a qualquer usuário autenticado.
/// Demonstra o uso básico de [Authorize] sem restrições de role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // JUNIOR: Requer estar logado. Qualquer role é aceita.
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido - requer autenticação.
    /// Retorna informações do usuário atual extraídas das claims.
    /// </summary>
    /// <returns>Email do usuário e lista de roles</returns>
    [HttpGet]
    public IActionResult Get()
    {
        // JUNIOR: User é um ClaimsPrincipal - contém todas as informações do usuário
        // User.Identity é a identidade autenticada (vinda do cookie)
        var userName = User.Identity?.Name;

        // JUNIOR: Como Roles são Claims, podemos acessá-las assim:
        // Filtramos a coleção Claims procurando as do tipo ClaimTypes.Role
        // Este é o padrão para acessar roles programaticamente (sem usar User.IsInRole)
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // JUNIOR: Alternativa mais simples para verificar uma role específica:
        // User.IsInRole("Admin") retorna true se o usuário tem role "Admin"

        return Ok(new
        {
            message = "Recurso protegido",
            user = userName,
            roles
        });
    }
}
