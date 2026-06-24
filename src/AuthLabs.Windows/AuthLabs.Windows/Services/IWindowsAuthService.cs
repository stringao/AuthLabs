using System.Security.Claims;

namespace AuthLabs.Windows.Services;

/// <summary>
/// Interface para o servico de autenticacao Windows.
/// Fornece metodos para manipular claims e roles baseadas em Windows/AD.
/// </summary>
/// <remarks>
/// JUNIOR: Esta interface define o contrato para operacoes de autenticacao Windows.
/// Em uma aplicacao real, ela seria implementada para consultar o Active Directory
/// via LDAP (Lightweight Directory Access Protocol) ou ADSI (Active Directory Service Interfaces).
/// A interface usa ClaimsPrincipal porque e o modelo padrao do ASP.NET Core para
/// representar usuarios autenticados - cada claim e uma declaracao sobre o usuario
/// (nome, grupo, role, etc).
/// </remarks>
public interface IWindowsAuthService
{
    /// <summary>
    /// Obtem o nome do usuario Windows atual.
    /// </summary>
    /// <param name="principal">
    /// O principal do usuario atual (contem todas as claims do usuario).
    /// </param>
    /// <returns>
    /// O nome do usuario Windows (formato: DOMINIO\Usuario) ou null se nao autenticado.
    /// </returns>
    /// <remarks>
    /// JUNIOR: ClaimsPrincipal e uma collection de ClaimsIdentity. No caso de Windows Authentication,
    /// a identity contem o nome do usuario no formato "DOMINIO\NomeUsuario".
    /// Exemplo: "CONTOSO\john.doe"
    /// </remarks>
    string? GetCurrentUserName(ClaimsPrincipal principal);

    /// <summary>
    /// Obtem o tipo de autenticacao utilizado (Kerberos ou NTLM).
    /// </summary>
    /// <param name="principal">O principal do usuario atual.</param>
    /// <returns>
    /// Uma string indicando o tipo de autenticacao: "Kerberos" ou "NTLM", ou null.
    /// </returns>
    /// <remarks>
    /// JUNIOR: Windows Authentication suporta dois protocolos principais:
    /// - Kerberos: Preferido para sistemas modernos, usa tickets criptografados,
    ///   mais seguro, funciona em redes com relacao de confiança entre dominios.
    /// - NTLM: Legado, usa hash de senha, menos seguro, usado quando Kerberos
    ///   não esta disponivel (ex: clientes antigos ou redes sem relacao de confiança).
    /// O protocolo "Negotiate" (usado neste projeto) tenta usar Kerberos primeiro
    /// e cai para NTLM se necessario.
    /// </remarks>
    string? GetAuthenticationType(ClaimsPrincipal principal);

    /// <summary>
    /// Verifica se o usuario pertence a um grupo especifico do Active Directory.
    /// </summary>
    /// <param name="principal">O principal do usuario atual.</param>
    /// <param name="groupName">Nome do grupo AD a verificar (ex: "Domain Admins").</param>
    /// <returns>
    /// True se o usuario pertence ao grupo, False caso contrario ou se nao autenticado.
    /// </returns>
    /// <remarks>
    /// JUNIOR: Em producao, esta verificacao consultaria o Active Directory via LDAP.
    /// O LDAP usa portas padrao: 389 (LDAP) ou 636 (LDAPS - LDAP Seguro).
    /// Exemplo de grupo: "Domain Admins", "Enterprise Admins", "Schema Admins".
    /// IMPORTANTE: Esta operacao pode ser lenta se o AD estiver em outro site de rede,
    /// por isso e uma operacao assincrona (Async) para no caso real nao bloquear a thread.
    /// </remarks>
    Task<bool> IsInAdGroupAsync(ClaimsPrincipal principal, string groupName);

    /// <summary>
    /// Obtem todos os grupos do Active Directory do usuario atual.
    /// </summary>
    /// <param name="principal">O principal do usuario atual.</param>
    /// <returns>
    /// Uma lista com os nomes de todos os grupos AD do usuario.
    /// </returns>
    /// <remarks>
    /// JUNIOR: Cada usuario Windows pode pertencer a muitos grupos no AD.
    /// Grupos podem representar: unidades organizacionais (OU), grupos de seguranca,
    /// grupos de distribuicao, etc.
    /// Esta informacao e usada para authorization (permissao de acesso) baseado em grupos.
    /// Em producao, a consulta seria feita via LDAP query como:
    /// (&amp;(objectClass=user)(sAMAccountName=username)(memberOf=groupDN))
    /// </remarks>
    Task<IEnumerable<string>> GetUserAdGroupsAsync(ClaimsPrincipal principal);
}
