using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthPolicies = AuthLabs.Claims.Authorization.AuthorizationPolicies;

namespace AuthLabs.Claims.Controllers;

/// <summary>
/// Controller que demonstra proteção de endpoints com políticas de claims.
/// </summary>
/// <remarks>
/// JUNIOR: O que é um Controller?
/// Um Controller é uma classe que agrupa endpoints (métodos HTTP) relacionados.
/// Ele recebe requisições HTTP, processa-as e retorna respostas.
///
/// O sufixo "Controller" é uma convenção do ASP.NET Core.
/// A classe deve herdar de ControllerBase para controllers de API.
///
/// JUNIOR: O que são Claims?
/// Claims são declarações sobre o usuário. Cada claim tem:
/// - Type (tipo): o que está sendo declarado (ex: email, nome, permissão)
/// - Value (valor): o conteúdo da declaração (ex: "usuario@email.com", "true")
///
/// Exemplos de claims:
/// - ClaimTypes.Email = "usuario@empresa.com"
/// - "Document:Edit" = "true" (pode editar documentos)
/// - "Subscription:Tier" = "Premium" (nível de assinatura)
///
/// Claims são emitidos por um sistema de identidade (Identity Provider)
/// quando o usuário faz login, e são armazenados no cookie de autenticação.
/// </remarks>
/// <example>
/// Exemplo de fluxo de uma requisição protegida:
/// 1. Usuário faz login → recebe cookie com claims
/// 2. Usuário chama GET /api/protected/edit
/// 3. ASP.NET Core verifica o cookie → extrai claims → define User
/// 4. Atributo [Authorize] verifica se User satisfaz a política "CanEditDocuments"
/// 5. Se sim, o método Edit() é executado; se não, retorna 403 Forbidden
/// </example>
[ApiController]
[Route("api/[controller]")]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Retorna informações do usuário logado, incluindo todos os seus claims.
    /// </summary>
    /// <returns>Email do usuário e lista de todos os claims.</returns>
    /// <remarks>
    /// JUNIOR: Este endpoint mostra como acessar os claims do usuário.
    ///
    /// O objeto User (do tipo ClaimsPrincipal) está disponível em todo controller
    /// e representa o usuário autenticado da requisição atual.
    ///
    /// User.FindFirstValue() procura um claim específico pelo seu tipo.
    /// User.Claims retorna TODOS os claims do usuário.
    ///
    /// Este endpoint requer apenas que o usuário esteja autenticado ([Authorize]
    /// sem política específica), então qualquer usuário logado pode acessá-lo.
    /// </remarks>
    /// <response code="200">Retorna as informações do usuário com seus claims.</response>
    /// <response code="401">Retornado se o usuário não está autenticado.</response>
    [HttpGet]
    [Authorize]
    public IActionResult GetUserInfo()
    {
        // JUNIOR: FindFirstValue é um atalho para encontrar o primeiro claim
        // de um tipo específico. ClaimTypes.Email é uma constante que representa
        // o tipo "email" (na verdade é uma URL: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
        var email = User.FindFirstValue(ClaimTypes.Email);

        // JUNIOR: User.Claims é uma coleção de todos os claims do usuário.
        // Select projeta cada claim em um objeto anônimo com Type e Value.
        // ToList() materializa a query LINQ em uma lista.
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
    /// <returns>Mensagem de sucesso.</returns>
    /// <remarks>
    /// JUNIOR: Como funciona a proteção por política?
    ///
    /// [Authorize(Policy = "CanEditDocuments")] faz o seguinte:
    /// 1. Procura a política chamada "CanEditDocuments" nas configurações
    /// 2. Verifica todos os requisitos dessa política (neste caso, RequireClaim)
    /// 3. Se todos os requisitos são satisfeitos, executa o método
    /// 4. Se algum requisito falha, retorna 403 Forbidden
    ///
    /// A política "CanEditDocuments" foi definida no Program.cs como:
    /// policy.RequireClaim("Document:Edit", "true")
    ///
    /// Isso significa que o usuário DEVE ter um claim:
    /// - Com tipo = "Document:Edit"
    /// - Com valor = "true"
    /// </remarks>
    /// <response code="200">Retornado se o usuário tem permissão para editar.</response>
    /// <response code="403">Retornado se o usuário não tem o claim Document:Edit=true.</response>
    [HttpGet("edit")]
    [Authorize(Policy = AuthPolicies.CanEditDocuments)]
    public IActionResult Edit()
    {
        return Ok(new { message = "Acesso permitido: você pode editar documentos" });
    }

    /// <summary>
    /// Endpoint para excluir documentos. Requer claim Document:Delete=true.
    /// </summary>
    /// <returns>Mensagem de sucesso.</returns>
    /// <remarks>
    /// JUNIOR: Note que este é um endpoint SEPARADO de Edit.
    ///
    /// Em muitos sistemas, editar e excluir são permissões DIFERENTES.
    /// Um usuário pode ter permissão para editar um documento mas NÃO para excluí-lo.
    /// Isso segue o princípio do "menor privilégio":
    /// dê ao usuário apenas as permissões que ele realmente precisa.
    ///
    /// A política CanDeleteDocuments verifica o claim "Document:Delete" = "true".
    /// </remarks>
    /// <response code="200">Retornado se o usuário tem permissão para excluir.</response>
    /// <response code="403">Retornado se o usuário não tem o claim Document:Delete=true.</response>
    [HttpGet("delete")]
    [Authorize(Policy = AuthPolicies.CanDeleteDocuments)]
    public IActionResult Delete()
    {
        return Ok(new { message = "Acesso permitido: você pode excluir documentos" });
    }

    /// <summary>
    /// Endpoint administrativo para gerenciar usuários. Requer claim User:Manage=true.
    /// </summary>
    /// <returns>Mensagem de sucesso.</returns>
    /// <remarks>
    /// JUNIOR: Por que chamar de "admin"?
    /// Este endpoint é tipicamente usado por administradores do sistema
    /// que precisam gerenciar contas de usuários (criar, editar, excluir usuários).
    ///
    /// CUIDADO: Em aplicações reais, endpoints administrativos como este
    /// devem ter proteção EXTRA. Além da política, considere:
    /// - Verificar se o usuário é um admin específico
    /// - Logar todas as operações administrativas
    /// - Implementar auditoria (audit trail)
    /// - Usar autenticação em múltiplos fatores
    ///
    /// A política CanManageUsers verifica o claim "User:Manage" = "true".
    /// </remarks>
    /// <response code="200">Retornado se o usuário tem permissão administrativa.</response>
    /// <response code="403">Retornado se o usuário não tem o claim User:Manage=true.</response>
    [HttpGet("admin")]
    [Authorize(Policy = AuthPolicies.CanManageUsers)]
    public IActionResult Admin()
    {
        return Ok(new { message = "Acesso permitido: você pode gerenciar usuários" });
    }

    /// <summary>
    /// Endpoint exclusivo para usuários Premium. Requer claim Subscription:Tier=Premium.
    /// </summary>
    /// <returns>Mensagem de sucesso.</returns>
    /// <remarks>
    /// JUNIOR: O que são políticas baseadas em Subscription/Tier?
    ///
    /// Muitas aplicações usam um modelo de negócio onde diferentes níveis
    /// de assinatura (Free, Basic, Premium, Enterprise) dão acesso a
    /// recursos diferentes.
    ///
    /// Neste exemplo:
    /// - Usuários Free/Basic: NÃO podem acessar este endpoint
    /// - Usuários Premium: PODEM acessar este endpoint
    ///
    /// Este é um padrão comum para monetização de APIs/serviços.
    /// O claim "Subscription:Tier" indica qual plano o usuário tem.
    ///
    /// A política IsPremiumUser verifica o claim "Subscription:Tier" = "Premium".
    /// </remarks>
    /// <response code="200">Retornado se o usuário é Premium.</response>
    /// <response code="403">Retornado se o usuário não é Premium.</response>
    [HttpGet("premium")]
    [Authorize(Policy = AuthPolicies.IsPremiumUser)]
    public IActionResult Premium()
    {
        return Ok(new { message = "Acesso permitido: você é um usuário Premium" });
    }
}
