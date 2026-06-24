using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace AuthLabs.Windows.Services;

/// <summary>
/// Transforma claims do Windows/AD em roles da aplicacao.
/// Em producao real, isso consultaria o AD para mapear grupos.
/// </summary>
public class WindowsClaimsTransformer : IClaimsTransformation
{
    /// <summary>
    /// Transforma claims do principal Windows, adicionando roles baseadas
    /// no nome do usuario (simplificado para demonstracao).
    /// </summary>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return Task.FromResult(principal);

        var name = identity.Name;

        // Mapeamento simplificado para demo
        // Em producao, consultar AD para grupos do usuario
        if (name?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        }
        else
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        }

        // Adiciona claim de autenticacao Windows
        identity.AddClaim(new Claim("WindowsAuth", "true"));

        return Task.FromResult(principal);
    }
}
