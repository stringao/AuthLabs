using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Jwt.Controllers;

/// <summary>
/// Controller de exemplo com endpoints protegidos por JWT.
///
/// JUNIOR: "Protegido" significa que só usuários autenticados
/// podem acessar estes endpoints. O atributo [Authorize] no topo
/// da classe aplica proteção a TODOS os métodos deste controller.
///
/// Se o usuário não tiver token JWT válido, receberá 401 Unauthorized.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido básico - retorna informações do usuário.
    ///
    /// FLUXO:
    /// 1. middleware JWT valida token e extrai claims
    /// 2. Claims são adicionadas ao HttpContext.User
    /// 3. Controller lê User.Identity para obter nome do usuário
    ///
    /// JUNIOR: User é uma propriedade do ControllerBase.
    /// Ela contém as informações do usuário extraídas do JWT.
    /// User.Identity?.Name é o nome do usuário (ou null se não autenticado).
    /// User.Claims são TODAS as claims do token (roles, email, etc).
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        // User.Identity?.Name - o "?" significa que pode ser null
        // Se o usuário não estiver autenticado, Name será null
        var userName = User.Identity?.Name;

        // User.Claims contém todas as claims do token JWT
        // Selecionamos só Type e Value para retornar ao cliente
        // JUNIOR: Claims são pares chave-valor como:
        // (ClaimTypes.Name, "admin") ou (ClaimTypes.Role, "Admin")
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new
        {
            message = "Você está autenticado!",
            user = userName,
            claims
        });
    }

    /// <summary>
    /// Endpoint que requer role específica (Admin).
    ///
    /// FLUXO:
    /// 1. middleware JWT valida token
    /// 2. Authorization middleware verifica se usuário tem role "Admin"
    /// 3. Se não tiver, retorna 403 Forbidden
    /// 4. Se tiver, executa o método
    ///
    /// JUNIOR: [Authorize(Roles = "Admin")] é uma forma de
    /// autorização BASEADA EM ROLES. Além de estar autenticado,
    /// o usuário PRECISA ter a role "Admin".
    ///
    /// 403 Forbidden vs 401 Unauthorized:
    /// - 401 = você não está autenticado (não tem token válido)
    /// - 403 = você está autenticado, mas não tem permissão
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminOnly()
    {
        return Ok(new { message = "Área administrativa" });
    }
}