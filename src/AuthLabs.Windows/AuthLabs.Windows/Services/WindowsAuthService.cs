using System.Security.Claims;

namespace AuthLabs.Windows.Services;

/// <summary>
/// Implementacao do servico de autenticacao Windows.
/// Em producao real, consultaria o Active Directory para obter grupos e roles.
/// </summary>
/// <remarks>
/// JUNIOR: Esta classe implementa a interface IWindowsAuthService.
///
/// AUTENTICACAO WINDOWS (KERBEROS/NTLM):
/// =======================================
///
/// Windows Authentication e um mecanismo de "Single Sign-On" (SSO) que permite
/// usuarios logados no Windows acessarem recursos sem digitar senha novamente.
///
/// COMO FUNCIONA:
/// 1. Usuario faz login no Windows com suas credenciais do AD
/// 2. Credenciais sao automaticamente usadas para autenticar em aplicacoes
/// 3. Nao ha necessidade de formulario de login ou senhas separadas
///
/// KERBEROS (protocolo padrao):
/// - Usa "tickets" criptografados com chave secreta compartilhada
/// - Funciona com multiplos dominios/forestas via relacoes de confiança
/// - Requer Relacao de Confianca entre o servidor e o cliente
/// - Usa portas: 88 (UDP/TCP) para autenticacao
///
/// NTLM (fallback/legado):
/// - Usa desafio-resposta com hash da senha
/// - Funciona em redes simples sem relacao de confiança
/// - Menos seguro que Kerberos (nao suporta delegation, etc)
/// - Usado quando: cliente nao suporta Kerberos, sem relacao de confiança, ou servidor nao registrado no AD
///
/// NEGOTIATE:
/// - Protocolo que "negocia" automaticamente entre Kerberos e NTLM
/// - Tenta Kerberos primeiro; se falhar, usa NTLM
/// - Este projeto usa "Negotiate" para maxima compatibilidade
///
/// ALERTA WINDOWS-LINUX:
/// =====================
/// Este codigo funciona tanto no Windows quanto no Linux/Mac durante DEVELOPMENT,
/// mas a autenticacao Windows REAL (Kerberos/NTLM) SO funciona no Windows.
/// No Linux/Mac, o modulo Negotiate nao pode autenticar usuarios reais.
///
/// Em PRODUCAO:
/// - Windows: Use IIS ou Kestrel com Windows Authentication habilitada
/// - Linux: Use LDAP direto (nao Windows Auth) ou Kerberos standalone
/// </remarks>
public class WindowsAuthService : IWindowsAuthService
{
    /// <summary>
    /// Mapeamento simplificado para demonstracao.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Em producao, este mapeamento seria feito consultando o AD real.
    /// Para consultar AD em producao, voce usaria:
    /// - System.DirectoryServices (requer Windows)
    /// - System.DirectoryServices.Protocols (cross-platform LDAP)
    /// - Novell.Directory.Ldap.NETStandard (biblioteca LDAP pura)
    /// </remarks>
    private static readonly Dictionary<string, string[]> UserAdGroups = new()
    {
        { "Admin", new[] { "Domain Admins", "Enterprise Admins", "Schema Admins" } },
        { "User", new[] { "Domain Users", "Workstations" } }
    };

    /// <inheritdoc />
    /// <remarks>
    /// JUNIOR: ClaimsPrincipal e o "porta-retrato" do usuario no ASP.NET Core.
    /// Ele pode conter multiplas identidades (ex: Windows + Cookie + JWT simultaneos).
    /// principal.Identity da a identidade "primaria" - no nosso caso, a Windows Identity.
    /// principal.Identity?.Name retorna o nome do usuario no formato DOMINIO\Usuario.
    /// </remarks>
    public string? GetCurrentUserName(ClaimsPrincipal principal)
    {
        return principal.Identity?.Name;
    }

    /// <inheritdoc />
    /// <remarks>
    /// JUNIOR: AuthenticationType indica qual protocolo foi usado:
    /// - "Negotiate" = escolheu automaticamente entre Kerberos/NTLM
    /// - "Kerberos" = forcou Kerberos
    /// - "NTLM" = forcou NTLM (raro, geralmente e Negotiate)
    ///
    /// Esta informacao e util para debug e auditoria de seguranca.
    /// </remarks>
    public string? GetAuthenticationType(ClaimsPrincipal principal)
    {
        return principal.Identity?.AuthenticationType;
    }

    /// <inheritdoc />
    /// <remarks>
    /// JUNIOR: Implementacao de demonstracao que simula consulta AD.
    ///
    /// LOGICA DE SIMULACAO:
    /// - usernames contendo "Admin" sao mapeados para o grupo "Admin"
    /// - outros usernames sao mapeados para o grupo "User"
    ///
    /// EM PRODUCAO (codigo real que voce escreveria):
    /// <code>
    /// using System.DirectoryServices;
    /// // ou
    /// using System.DirectoryServices.Protocols;
    ///
    /// public Task&lt;bool&gt; IsInAdGroupAsync(ClaimsPrincipal principal, string groupName)
    /// {
    ///     var userName = GetCurrentUserName(principal);
    ///     using var searcher = new DirectorySearcher(
    ///         new DirectoryEntry("LDAP://DC=seudominio,DC=com")
    ///     );
    ///     searcher.Filter = $"(sAMAccountName={userName})";
    ///     searcher.PropertiesToLoad.Add("memberOf");
    ///
    ///     var result = searcher.FindOne();
    ///     if (result == null) return Task.FromResult(false);
    ///
    ///     var groups = result.Properties["memberOf"];
    ///     return Task.FromResult(groups.Contains(groupName));
    /// }
    /// </code>
    /// </remarks>
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

    /// <inheritdoc />
    /// <remarks>
    /// JUNIOR: Esta metodo retorna TODOS os grupos do AD do usuario.
    /// E util quando voce precisa verificar multiplas permissoes de uma vez,
    /// evitando multiplas chamadas ao AD (que sao lentas).
    ///
    /// O padrao aqui e "cache local" - primeiro tenta pegar todos os grupos,
    /// depois verifica em memoria qual grupo importa.
    ///
    /// IMPORTANTE: Groups no AD podem ser muito numerosos (centenas para alguns usuarios).
    /// Considere usar paginacao ou filtragem se a lista for muito grande.
    /// </remarks>
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
