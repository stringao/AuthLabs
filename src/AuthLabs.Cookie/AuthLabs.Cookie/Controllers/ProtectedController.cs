using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Cookie.Controllers;

/// <summary>
/// Controller com endpoints que exigem autenticação e/ou autorização.
///
/// DIFERENÇA ENTRE AUTENTICAÇÃO E AUTORIZAÇÃO:
/// - Autenticação: "Quem você é?" (você é o admin@authlabs.com?)
/// - Autorização: "O que você pode fazer?" (você pode acessar /admin?)
///
/// O atributo [Authorize] no topo da classe significa:
/// "Todos os endpoints aqui exigem que o usuário esteja autenticado"
///
/// Se o usuário não estiver logado, receberá 401 Unauthorized.
///
/// Para permitir acesso anônimo, use [AllowAnonymous] no método específico.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // TODOS os endpoints aqui exigem autenticação
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Retorna informações completas do usuário autenticado.
    ///
    /// FLUXO:
    /// 1. Middleware de autenticação extraiu usuário do cookie
    /// 2. Claims estão disponíveis em User.Claims
    /// 3. Retornamos as informações para o cliente
    ///
    /// JUNIOR: User é uma propriedade do ControllerBase.
    /// Ele contém:
    /// - User.Identity: informações básicas (Name, IsAuthenticated, AuthenticationType)
    /// - User.Claims: lista de TODAS as claims (roles, email, etc)
    ///
    /// Claims são "declarações sobre o usuário".
    /// Exemplo de claims que o Identity cria automaticamente:
    /// - ClaimTypes.Name = "admin"
    /// - ClaimTypes.Email = "admin@authlabs.com"
    /// - ClaimTypes.Role = "Admin"
    /// </summary>
    [HttpGet]
    public IActionResult GetUserInfo()
    {
        // User.Claims = todas as claims do usuário extraídas do cookie
        // Select cria um objeto anônimo só com Type e Value para retornar
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        // User.Identity contém informações básicas
        // O "?" significa nullable - pode ser null se não autenticado
        // (mas aqui estamos em endpoint [Authorize], então sempre terá valor)
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
    /// Endpoint exclusivo para usuários com role Admin.
    ///
    /// FLUXO DE AUTORIZAÇÃO:
    /// 1. Usuário tenta acessar /api/protected/admin
    /// 2. Middleware verifica se há identity (autenticação)
    /// 3. Middleware verifica se identity tem role "Admin" (autorização)
    /// 4. Se não tiver, retorna 403 Forbidden
    ///
    /// [Authorize(Roles = "Admin")] = só quem tem role Admin pode acessar
    ///
    /// JUNIOR: Roles são "papéis" - agrupa permissões.
    /// Ex: Admin = pode gerenciar usuários, ver relatórios, etc.
    ///     User = pode só ler dados
    ///     Guest = pode só ver dados públicos
    ///
    /// Se você precisar de múltiplas roles (OR), separe por vírgula:
    /// [Authorize(Roles = "Admin,Manager")] = Admin OU Manager podem acessar
    ///
    /// Se precisar de múltiplas roles (AND), use políticas (Policies)
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
    /// Endpoint exclusivo para usuários com role Manager.
    ///
    /// MESMA LÓGICA do Admin, mas para Managers.
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
    /// Endpoint para qualquer usuário autenticado.
    ///
    /// JUNIOR: Este endpoint usa apenas [Authorize] no nível do controller,
    /// sem especificar Roles. Qualquer usuário logado pode acessar.
    ///
    /// Diferente do Admin e Manager que têm [Authorize(Roles = "Xxx")].
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