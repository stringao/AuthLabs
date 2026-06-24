using AuthLabs.Shared.Models;

namespace AuthLabs.Rbac.Services;

/// <summary>
/// Interface para serviço de gerenciamento de roles.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Obtém usuário pelo email.
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Obtém as roles de um usuário.
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(User user);
}
