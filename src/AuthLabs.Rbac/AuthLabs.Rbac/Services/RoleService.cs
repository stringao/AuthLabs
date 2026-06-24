using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthLabs.Rbac.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de roles.
/// </summary>
public class RoleService : IRoleService
{
    private readonly UserManager<User> _userManager;

    public RoleService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    /// <inheritdoc />
    public async Task<IList<string>> GetUserRolesAsync(User user)
    {
        return await _userManager.GetRolesAsync(user);
    }
}
