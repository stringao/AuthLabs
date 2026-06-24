namespace AuthLabs.OAuth.Infrastructure;

/// <summary>
/// Configuração de um provider OAuth/OIDC.
/// </summary>
public class OAuthProviderConfig
{
    public string ProviderName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string UserInfoEndpoint { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = string.Empty;
}

/// <summary>
/// Configurações OAuth da aplicação.
/// </summary>
public class OAuthSettings
{
    public Dictionary<string, OAuthProviderConfig> Providers { get; set; } = new();
}