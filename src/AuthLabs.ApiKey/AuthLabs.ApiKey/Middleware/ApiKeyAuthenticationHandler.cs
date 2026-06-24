using System.Security.Claims;
using AuthLabs.ApiKey.Services;

namespace AuthLabs.ApiKey.Middleware;

/// <summary>
/// Middleware de autenticacao via API Key.
/// Intercepta todas as requisicoes e valida a API Key no header X-Api-Key.
/// </summary>
/// <remarks>
/// JUNIOR: O que e um Middleware?
/// - Middleware e um componente que fica no "meio" do pipeline de requisicoes
/// - Executa ANTES do controller (ou BLOQUEIA o acesso)
/// - Cada requisicao passa por todos os middlewares em ordem
/// - Se um middleware nao chamar _next, a requisicao para ali
///
/// JUNIOR: Por que usar middleware para autenticacao?
/// - Centraliza a logica de autenticacao em um unico lugar
/// - Todas as requisicoes passam por aqui automaticamente
/// - Nao precisa decorar cada controller/acao com [Authorize]
/// </remarks>
public class ApiKeyAuthenticationHandler
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Nome do header HTTP onde a API Key deve ser enviada.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Convencoes de headers:
    /// - Headers customizados seguem formato X-Nome
    /// - X-Api-Key e convencao comum para API Keys
    /// - Poderia ser qualquer nome, mas padronizar ajuda
    /// </remarks>
    private const string ApiKeyHeaderName = "X-Api-Key";

    /// <summary>
    /// Construtor que recebe o proximo middleware no pipeline.
    /// </summary>
    /// <param name="next">Delegate que representa o proximo middleware/endpoint</param>
    public ApiKeyAuthenticationHandler(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Metodo principal que processa cada requisicao HTTP.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Ciclo de vida de uma requisicao:
    /// 1. Cliente envia requisicao com header X-Api-Key
    /// 2. Middleware extrai e valida a key
    /// 3. Se valida, cria ClaimsPrincipal e adiciona ao HttpContext
    /// 4. Chama _next() para continuar o pipeline
    /// 5. Controller recebe requisicao com contexto de autenticacao
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // JUNIOR: HttpContext.Request.Path e o caminho da URL (ex: /api/users)
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // ============================================================
        // JUNIOR: ROTEAMENTO BYPASS (Skip authentication para algumas rotas)
        // ============================================================

        /// <summary>
        /// Rotas publicas que NAO precisam de autenticacao.
        /// </summary>
        /// <remarks>
        /// JUNIOR: Por que skipar /swagger e /health?
        /// - /swagger: documentacao da API - precisa ser acessivel para devs
        /// - /health: usado por load balancers e orquestradores (K8s, Docker)
        /// - Se этих rotas precisarem de auth, você terá problemas de monitoramento
        /// </remarks>
        if (path.StartsWith("/swagger") || path.StartsWith("/health"))
        {
            await _next(context); // JUNIOR: Continua o pipeline sem autenticar
            return;
        }

        // ============================================================
        // JUNIOR: EXTRACAO DA API KEY DO HEADER
        // ============================================================

        // TryGetValue retorna false se header nao existir
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            // JUNIOR: 401 Unauthorized - cliente precisa se autenticar
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key nao fornecida" });
            return;
        }

        // JUNIOR: extractedApiKey e StringValues, precisa converter para string
        var apiKey = extractedApiKey.ToString();

        // ============================================================
        // JUNIOR: VALIDACAO DA API KEY
        // ============================================================

        // Validação de seguranca: não aceitar string vazia
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key nao pode ser vazia" });
            return;
        }

        // Consulta o servico para verificar se a key e valida
        // GetApiKeyInfoAsync retorna null se: key inexistente, hash nao bate, expirada, inativa
        var apiKeyInfo = await apiKeyService.GetApiKeyInfoAsync(apiKey);

        if (apiKeyInfo == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key invalida ou expirada" });
            return;
        }

        // ============================================================
        // JUNIOR: CRIACAO DA IDENTIDADE DO USUARIO (Claims)
        // ============================================================

        /// <summary>
        /// Claims sao "declaracoes" sobre o usuario.
        /// </summary>
        /// <remarks>
        /// JUNIOR: O que sao Claims?
        /// - Claim = "Eu sou o usuario X, com role Y"
        /// - Sao usados para AUTHORIZATION (nao autenticacao)
        /// - O sistema verifica os claims para permitir/negar acesso
        /// - Claims principais: Name, Role
        /// - Claims customizados: apikey_id, client_name, scope
        /// </remarks>
        var claims = new List<Claim>
        {
            // ClaimTypes.Name e usado por User.Identity.Name
            new(ClaimTypes.Name, apiKeyInfo.ClientName),

            // ClaimTypes.Role e usado por [Authorize(Roles = "...")]
            new(ClaimTypes.Role, apiKeyInfo.Role),

            // Claim customizado para guardar o ID da API Key
            new("apikey_id", apiKeyInfo.Id.ToString()),

            // Claim customizado para guardar o nome do cliente
            new("client_name", apiKeyInfo.ClientName)
        };

        // ============================================================
        // JUNIOR: ADICAO DOS SCOPES COMO CLAIMS
        // ============================================================

        // Cada scope vira um claim separado "scope:read", "scope:write", etc.
        foreach (var scope in apiKeyInfo.Scopes)
        {
            // JUNIOR: Claims podem ter qualquer tipo (nao so ClaimTypes)
            // "scope" e o tipo do claim, o valor e o nome do scope
            claims.Add(new Claim("scope", scope));
        }

        // ============================================================
        // JUNIOR: CONFIGURANDO O PRINCIPAL NO CONTEXTO HTTP
        // ============================================================

        /// <summary>
        /// ClaimsIdentity = identidade do usuario com seus claims
        /// ClaimsPrincipal = pode conter multiplas identidades
        /// </summary>
        /// <remarks>
        /// JUNIOR: Por que "ApiKey" como segundo parametro?
        /// - E o nome do scheme de autenticacao
        /// - Útil quando temos multiplos metodos de login (JWT, Cookie, etc)
        /// - Depois podemos usar scheme para identificar de onde veio a auth
        /// </remarks>
        var identity = new ClaimsIdentity(claims, "ApiKey");

        // ClaimsPrincipal e o que voce acessa com HttpContext.User
        // Controllers podem ler User.Claims para ver quem esta acessando
        context.User = new ClaimsPrincipal(identity);

        // ============================================================
        // JUNIOR: CONTINUANDO O PIPELINE
        // ============================================================

        // Chama o proximo middleware - a requisicao continuarah para o controller
        await _next(context);
    }
}

/// <summary>
/// Classe de extensoes para adicionar o middleware ao pipeline.
/// </summary>
/// <remarks>
/// JUNIOR: O que sao Extension Methods?
/// - Metodos estaticos que "adicionam" funcionalidade a tipos existentes
/// - Usam a palavra-chave "this" no primeiro parametro
/// - Permitem escrever: app.UseApiKeyAuthentication() em vez de UseMiddleware&lt;...&gt;()
/// - Sao apenas "syntax sugar" - fica mais legivel
/// </remarks>
public static class ApiKeyAuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Adiciona o middleware de autenticacao API Key ao pipeline da aplicacao.
    /// </summary>
    /// <param name="builder">IApplicationBuilder do ASP.NET Core</param>
    /// <returns>O mesmo IApplicationBuilder para encadeamento</returns>
    /// <example>
    /// // Uso no Program.cs:
    /// app.UseApiKeyAuthentication();
    /// </example>
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        // UseMiddleware e o metodo interno que registra o middleware no pipeline
        return builder.UseMiddleware<ApiKeyAuthenticationHandler>();
    }
}
