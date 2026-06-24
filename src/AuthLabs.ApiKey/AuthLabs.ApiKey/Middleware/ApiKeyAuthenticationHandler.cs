using System.Security.Claims;
using AuthLabs.ApiKey.Services;

namespace AuthLabs.ApiKey.Middleware;

/// <summary>
/// Middleware de autenticacao via API Key.
/// Le o header X-Api-Key, valida contra o banco e adiciona identidade ao contexto.
/// Ignora /swagger e /health.
/// </summary>
public class ApiKeyAuthenticationHandler
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyAuthenticationHandler(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip autenticacao para /swagger e /health
        if (path.StartsWith("/swagger") || path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        // Tenta obter a API Key do header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key nao fornecida" });
            return;
        }

        var apiKey = extractedApiKey.ToString();
        var apiKeyInfo = await apiKeyService.GetApiKeyInfoAsync(apiKey);

        if (apiKeyInfo == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key invalida ou expirada" });
            return;
        }

        // Cria claims baseados na API Key
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, apiKeyInfo.ClientName),
            new(ClaimTypes.Role, apiKeyInfo.Role),
            new("apikey_id", apiKeyInfo.Id.ToString()),
            new("client_name", apiKeyInfo.ClientName)
        };

        // Adiciona scopes como claims
        foreach (var scope in apiKeyInfo.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}

/// <summary>
/// Extensao para adicionar o middleware ao pipeline.
/// </summary>
public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthenticationHandler>();
    }
}
