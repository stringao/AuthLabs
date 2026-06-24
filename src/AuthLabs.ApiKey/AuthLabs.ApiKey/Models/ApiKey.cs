namespace AuthLabs.ApiKey.Models;

/// <summary>
/// Modelo de API Key - armazena o hash da key, nunca a key em si.
/// </summary>
public class ApiKey
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty; // hash da API key
    public string ClientName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ApiKeyScope> Scopes { get; set; } = new();
    public string Role { get; set; } = "User"; // Role associada à API key
}
