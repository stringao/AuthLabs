using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace AuthLabs.Windows.Services;

/// <summary>
/// Transforma claims do Windows/AD em roles da aplicacao.
/// Em producao real, isso consultaria o AD para mapear grupos.
/// </summary>
/// <remarks>
/// JUNIOR: Claims Transformation e um conceito importante no ASP.NET Core.
///
/// O PROBLEMA:
/// Quando o Windows Authentication autentica um usuario, ele cria uma ClaimsIdentity
/// com apenas informacoes basicas (nome, dominio). Mas sua aplicacao provavelmente
/// precisa de informacoes de ROLE (admin, user, manager, etc).
///
/// A SOLUCAO:
/// IClaimsTransformation e um middleware que permite "enriquecer" o principal
/// apos a autenticacao. Ele e chamado automaticamente apos cada autenticacao.
///
/// COMO FUNCIONA ESTA CLASSE:
/// 1. Recebe o ClaimsPrincipal do Windows (ja autenticado)
/// 2. Extrai informacoes (ex: nome do usuario)
/// 3. Consulta AD ou outro fonte de dados para obter grupos/roles
/// 4. Adiciona novas claims com as roles ao principal
/// 5. Retorna o principal "enriquecido"
///
/// FLUXO DE EXECUCAO:
/// Request -&gt; Authentication Middleware -&gt; [WindowsAuth] -&gt; ClaimsTransformer.TransformAsync() -&gt; Authorization Middleware -&gt; Controller
///
/// MULTITHREADING:
/// Esta classe e registrada como Scoped no DI, mas o TransformAsync e chamado
/// para cada request. A implementacao atual e thread-safe porque nao mantem
/// estado de instancia (somente ler o dicionario estatico UserAdGroups).
/// </remarks>
public class WindowsClaimsTransformer : IClaimsTransformation
{
    /// <summary>
    /// Transforma claims do principal Windows, adicionando roles baseadas
    /// no nome do usuario (simplificado para demonstracao).
    /// </summary>
    /// <param name="principal">
    /// O principal do usuario apos autenticacao Windows.
    /// Contem claims basicas: Name, AuthenticationType, IsAuthenticated, etc.
    /// </param>
    /// <returns>
    /// O mesmo principal com claims adicionais de Role incluidas.
    /// </returns>
    /// <remarks>
    /// JUNIOR: Esta e a unica interface que voce precisa implementar para
    /// adicionar rolesbase em Windows Authentication.
    ///
    /// EXEMPLO DO QUE ACONTECE:
    /// ANTES de TransformAsync:
    ///   Claims: [
    ///     ClaimTypes.Name = "CONTOSO\john.doe"
    ///     ClaimTypes.AuthenticationType = "Negotiate"
    ///   ]
    ///
    /// DEPOIS de TransformAsync:
    ///   Claims: [
    ///     ClaimTypes.Name = "CONTOSO\john.doe"
    ///     ClaimTypes.AuthenticationType = "Negotiate"
    ///     ClaimTypes.Role = "User"  // &lt;-- Adicionado pelo transformer!
    ///     "WindowsAuth" = "true"    // &lt;-- Custom claim
    ///   ]
    ///
    /// NOTA SOBRE CLAIMTYPES.ROLE:
    /// O ASP.NET Core reconhece claims com tipo ClaimTypes.Role como roles.
    /// Entao User.IsInRole("Admin") funciona automaticamente!
    /// </remarks>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return Task.FromResult(principal);

        var name = identity.Name;

        // Mapeamento simplificado para demo
        // Em producao, consultar AD para grupos do usuario
        if (name?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true)
        {
            // JUNIOR: ClaimTypes.Role e o tipo padrao de claim para roles no ASP.NET Core.
            // Quando voce usa [Authorize(Roles = "Admin")], o ASP.NET Core procura
            // claims deste tipo com valor "Admin".
            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        }
        else
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        }

        // Adiciona claim de autenticacao Windows
        // JUNIOR: Claim personalizado para identificar que a autenticacao foi via Windows.
        // Esta claim e util para logs, debug, ou logica de negocio que precisa
        // saber como o usuario foi autenticado.
        identity.AddClaim(new Claim("WindowsAuth", "true"));

        return Task.FromResult(principal);
    }
}
