namespace AuthLabs.Shared.Models;

/// <summary>
/// Entidade para armazenar refresh tokens (usado em JWT e Cookie).
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Navegação para o usuário dono do token.
    /// </summary>
    public User User { get; set; } = null!;
}