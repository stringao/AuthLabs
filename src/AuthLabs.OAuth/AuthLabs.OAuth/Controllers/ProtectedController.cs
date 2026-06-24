using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.OAuth.Controllers;

/// <summary>
/// Controller com endpoints que demonstram proteção por autenticação e autorização.
/// </summary>
/// <remarks>
/// JUNIOR: Por que este controller é importante?
///
/// Ele demonstra como proteger recursos usando:
/// 1. [Authorize] - requer autenticação
/// 2. [AllowAnonymous] - permite acesso público
/// 3. Claims-based authorization - verifica claims específicas
///
/// DIFERENÇA ENTRE AUTENTICAÇÃO E AUTORIZAÇÃO:
/// -------------------------------------------------------
/// AUTENTICAÇÃO (Authentication):
/// - "Você está logado?"
/// - [Authorize] sem parâmetros verifica apenas se há usuário
/// - Retorna 401 Unauthorized se não estiver autenticado
///
/// AUTORIZAÇÃO (Authorization):
/// - "Você tem permissão para acessar este recurso?"
/// - [Authorize(Roles = "Admin")] verifica se tem role Admin
/// - [Authorize(Policy = "RequireEmail")] verifica claims específicas
/// - Retorna 403 Forbidden se autenticado mas sem permissão
/// -------------------------------------------------------
///
/// STATUS HTTP IMPORTANTES:
/// - 401 Unauthorized: "Você não está logado" (autenticação falhou)
/// - 403 Forbidden: "Você está logado, mas não pode fazer isso" (autorização falhou)
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]  // TODOS os endpoints deste controller requerem autenticação por padrão
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint público - não requer autenticação.
    /// </summary>
    /// <returns>Mensagem de status e timestamp.</returns>
    /// <remarks>
    /// JUNIOR: [AllowAnonymous] sobrepõe [Authorize] da classe!
    ///
    /// Mesmo que ProtectedController tenha [Authorize] no topo,
    /// este endpoint é acessível sem login porque tem [AllowAnonymous].
    ///
    /// Útil para:
    /// - Páginas "sobre" ou "contato"
    /// - Endpoints de health check
    /// - Documentação pública
    /// </remarks>
    [HttpGet("public")]
    [AllowAnonymous]  // Permite acesso sem autenticação, sobrepõe [Authorize]
    public IActionResult Public()
    {
        return Ok(new { message = "Este endpoint é público", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Endpoint protegido - requer autenticação.
    /// </summary>
    /// <returns>Informações do usuário autenticado.</returns>
    /// <remarks>
    /// JUNIOR: Este método mostra o poder de [Authorize]
    ///
    /// Se você chamar este endpoint SEM estar logado:
    /// - ASP.NET Core retorna HTTP 401 Unauthorized automaticamente
    /// - O método nem chega a executar
    ///
    /// Se você ESTIVER logado:
    /// - O método executa normalmente
    /// - User.Identity.IsAuthenticated será true
    /// - User.Identity.Name terá o nome do usuário
    /// </remarks>
    [HttpGet("secure")]
    public IActionResult Secure()
    {
        return Ok(new
        {
            message = "Este endpoint requer autenticação",
            user = User.Identity?.Name,  // Nome do usuário (do provider OAuth)
            isAuthenticated = User.Identity?.IsAuthenticated  // Boolean: está logado?
        });
    }

    /// <summary>
    /// Endpoint que retorna perfil completo do usuário com todas as claims.
    /// Requer autenticação (herdado de [Authorize] da classe).
    /// </summary>
    /// <returns>Perfil do usuário com claims.</returns>
    /// <remarks>
    /// JUNIOR: Claims - informações sobre o usuário
    ///
    /// Claims são pares de chave-valor que descrevem o usuário.
    /// Exemplos de claims que você pode receber do Google:
    /// - "sub": identificador único do Google
    /// - "email": email do usuário
    /// - "name": nome de exibição
    /// - "picture": URL da foto de perfil
    /// - "email_verified": se o email foi verificado
    ///
    /// O loop User.Claims.Select() mostra TODAS as claims.
    /// Útil para debugging e para entender o que o provider enviou.
    /// </remarks>
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        // User.Claims é uma coleção de todas as claims do usuário
        // Claims podem vir do OAuth provider E do seu Identity local
        var claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();

        return Ok(new
        {
            message = "Perfil do usuário",
            user = User.Identity?.Name,  // Nome de exibição
            email = User.FindFirst("email")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            provider = User.FindFirst(System.Security.Claims.ClaimTypes.AuthenticationMethod)?.Value,
            claims  // Lista completa de claims - bom para debug!
        });
    }

    /// <summary>
    /// Retorna os tokens OAuth/OIDC do usuário (access_token, refresh_token).
    /// Útil para debugging - NUNCA retorne tokens em produção!
    /// </summary>
    /// <returns>Tokens armazenados na sessão.</returns>
    /// <remarks>
    /// JUNIOR: Tokens - a moeda da autenticação moderna
    ///
    /// Tipos de tokens em OAuth/OIDC:
    ///
    /// 1. ACCESS_TOKEN:
    ///    - Usado para acessar APIs em nome do usuário
    ///    - Tem curta duração (geralmente 1 hora)
    ///    - Qualquer pessoa com o token pode acessar os recursos
    ///    - O recurso (API) valida o token antes de responder
    ///
    /// 2. REFRESH_TOKEN:
    ///    - Usado para obter um novo access_token quando expira
    ///    - Tem longa duração (dias ou semanas)
    ///    - Deve ser guardado em local seguro (HTTP-only cookie é ideal)
    ///    - Pode ser revogado pelo usuário a qualquer momento
    ///
    /// 3. ID_TOKEN (específico OIDC):
    ///    - JWT que contém informações do usuário (claims)
    ///    - Prova que o usuário autenticou com o provider
    ///    - Pode ser validado localmente (não precisa chamar API)
    ///    - Contém: sub (subject), email, name, aud (audience), iss (issuer), exp, iat
    ///
    /// SEGURANÇA: Tokens em apps web:
    /// - Access Token: pode ser guardado em memória (JavaScript)
    /// - Refresh Token: ARMAZENAR EM HTTP-ONLY COOKIE ONLY!
    ///   (JavaScript não pode acessar HTTP-only cookies - proteção XSS)
    ///
    /// ATENÇÃO: Este endpoint retorna tokens para DEBUGGING!
    /// Em produção, NUNCA exponha tokens na resposta HTTP.
    /// </remarks>
    [HttpGet("tokens")]
    public async Task<IActionResult> GetTokens()
    {
        // AuthenticateAsync() obtém o resultado da autenticação atual
        // Properties contém os tokens se SaveTokens = true na configuração
        var authenticateResult = await HttpContext.AuthenticateAsync();
        if (!authenticateResult.Succeeded)
        {
            return Unauthorized(new { message = "Não autenticado" });
        }

        // Tokens são armazenados em Properties.Items
        // A chave segue o padrão ".Token.{token_type}"
        var tokens = new Dictionary<string, string?>();

        if (authenticateResult.Properties?.Items.ContainsKey(".Token.access_token") == true)
        {
            tokens["access_token"] = authenticateResult.Properties.Items[".Token.access_token"];
        }
        if (authenticateResult.Properties?.Items.ContainsKey(".Token.refresh_token") == true)
        {
            tokens["refresh_token"] = authenticateResult.Properties.Items[".Token.refresh_token"];
        }

        return Ok(new { tokens });
    }
}
