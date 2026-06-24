namespace AuthLabs.ApiKey.Models;

/// <summary>
/// Escopo associado a uma API Key (read, write, delete).
/// </summary>
public class ApiKeyScope
{
    public int Id { get; set; }
    public int ApiKeyId { get; set; }
    public ApiKey ApiKey { get; set; } = null!;
    public string Scope { get; set; } = string.Empty; // read, write, delete
}
