using System.Security.Claims;

namespace AuthLabs.Windows.Services;

/// <summary>
/// Interface para servico de autenticacao Windows.
/// Fornece metodos para manipular claims e roles baseadas em Windows/AD.
/// </summary>
public interface IWindowsAuthService
{
    /// <summary>
    /// Obtem o nome do usuario Windows atual.
    /// </summary>
    string? GetCurrentUserName(ClaimsPrincipal principal);

    /// <summary>
    /// Obtem o tipo de autenticacao (Kerberos/NTLM).
    /// </summary>
    string? GetAuthenticationType(ClaimsPrincipal principal);

    /// <summary>
    /// Verifica se o usuario pertence a um grupo específico do AD.
    /// </summary>
    Task<bool> IsInAdGroupAsync(ClaimsPrincipal principal, string groupName);

    /// <summary>
    /// Obtem todos os grupos do AD do usuario.
    /// </summary>
    Task<IEnumerable<string>> GetUserAdGroupsAsync(ClaimsPrincipal principal);
}
