namespace AuthLabs.Claims.Services;

/// <summary>
/// Implementação do serviço de claims.
/// </summary>
public class ClaimsService : IClaimsService
{
    private static readonly Dictionary<string, Dictionary<string, string>> UserClaims = new()
    {
        ["admin@authlabs.com"] = new Dictionary<string, string>
        {
            ["Document:Edit"] = "true",
            ["Document:Delete"] = "true",
            ["User:Manage"] = "true",
            ["Subscription:Tier"] = "Premium"
        },
        ["manager@authlabs.com"] = new Dictionary<string, string>
        {
            ["Document:Edit"] = "true",
            ["Subscription:Tier"] = "Standard"
        },
        ["user@authlabs.com"] = new Dictionary<string, string>
        {
            ["Document:Edit"] = "true",
            ["Subscription:Tier"] = "Basic"
        },
        ["guest@authlabs.com"] = new Dictionary<string, string>()
    };

    public Dictionary<string, string> GetUserClaims(string email)
    {
        return UserClaims.TryGetValue(email, out var claims) ? claims : new Dictionary<string, string>();
    }
}
