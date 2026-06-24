namespace AuthLabs.Claims.Services;

/// <summary>
/// Interface para serviço de claims.
/// </summary>
public interface IClaimsService
{
    /// <summary>
    /// Obtém os claims de um usuário específico.
    /// </summary>
    Dictionary<string, string> GetUserClaims(string email);
}
