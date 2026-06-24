using System.Security.Claims;

namespace AuthLabs.Windows.Services;

/// <summary>
/// Implementacao do servico de autenticacao Windows.
/// Em producao real, consultaria o Active Directory para obter grupos e roles.
/// </summary>
public class WindowsAuthService : IWindowsAuthService
{
    // Mapeamento simplificado para demonstracao
    // Em producao, Consultar AD via System.DirectoryServices
    private static readonly Dictionary<string, string[]> UserAdGroups = new()
    {
        { "Admin", new[] { "Domain Admins", "Enterprise Admins", "Schema Admins" } },
        { "User", new[] { "Domain Users", "Workstations" } }
    };

    public string? GetCurrentUserName(ClaimsPrincipal principal)
    {
        return principal.Identity?.Name;
    }

    public string? GetAuthenticationType(ClaimsPrincipal principal)
    {
        return principal.Identity?.AuthenticationType;
    }

    public Task<bool> IsInAdGroupAsync(ClaimsPrincipal principal, string groupName)
    {
        var userName = GetCurrentUserName(principal);
        if (string.IsNullOrEmpty(userName))
            return Task.FromResult(false);

        // Simula consulta AD - em producao usaria LDAP/ADSI
        var normalizedUserName = userName.Contains("Admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "User";

        if (UserAdGroups.TryGetValue(normalizedUserName, out var groups))
        {
            return Task.FromResult(groups.Contains(groupName, StringComparer.OrdinalIgnoreCase));
        }

        return Task.FromResult(false);
    }

    public Task<IEnumerable<string>> GetUserAdGroupsAsync(ClaimsPrincipal principal)
    {
        var userName = GetCurrentUserName(principal);
        if (string.IsNullOrEmpty(userName))
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

        // Simula consulta AD
        var normalizedUserName = userName.Contains("Admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "User";

        if (UserAdGroups.TryGetValue(normalizedUserName, out var groups))
        {
            return Task.FromResult<IEnumerable<string>>(groups);
        }

        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }
}
