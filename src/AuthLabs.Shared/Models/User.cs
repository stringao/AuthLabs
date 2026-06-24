using Microsoft.AspNetCore.Identity;

namespace AuthLabs.Shared.Models;

/// <summary>
/// Entidade base de usuário para todos os padrões de autenticação.
/// </summary>
public class User : IdentityUser<int>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}