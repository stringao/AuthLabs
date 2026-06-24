using AuthLabs.ApiKey.Models;

namespace AuthLabs.ApiKey.Services;

/// <summary>
/// Interface para validacao e consulta de API Keys.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Valida uma API Key e retorna o clientName se valida.
    /// </summary>
    Task<string?> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// Retorna informacoes da API Key se valida.
    /// </summary>
    Task<ApiKeyInfo?> GetApiKeyInfoAsync(string apiKey);
}
