using Microsoft.AspNetCore.Authorization;

namespace AuthLabs.Claims.Authorization;

/// <summary>
/// Requisito customizado para verificar claim específica.
/// </summary>
public class CustomClaimRequirement : IAuthorizationRequirement
{
    public string ClaimType { get; }
    public string ClaimValue { get; }

    public CustomClaimRequirement(string claimType, string claimValue)
    {
        ClaimType = claimType;
        ClaimValue = claimValue;
    }
}

/// <summary>
/// Políticas de autorização pré-definidas.
/// </summary>
public static class AuthorizationPolicies
{
    public const string CanEditDocuments = "CanEditDocuments";
    public const string CanDeleteDocuments = "CanDeleteDocuments";
    public const string CanManageUsers = "CanManageUsers";
    public const string IsPremiumUser = "IsPremiumUser";
}
