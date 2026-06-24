namespace AuthLabs.ApiKey.Models;

/// <summary>
/// DTO com informacoes da API Key para retorno ao cliente.
/// </summary>
public class ApiKeyInfo
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
