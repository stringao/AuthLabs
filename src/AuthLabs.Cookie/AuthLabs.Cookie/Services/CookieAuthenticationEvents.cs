using Microsoft.AspNetCore.Authentication.Cookies;

namespace AuthLabs.Cookie.Services;

/// <summary>
/// Eventos customizados para autenticação por cookie.
/// Permite implementar lógica adicional durante o processo de autenticação.
/// </summary>
public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly ILogger<CustomCookieAuthenticationEvents> _logger;

    public CustomCookieAuthenticationEvents(ILogger<CustomCookieAuthenticationEvents> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when the authentication ticket has been validated.
    /// </summary>
    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        _logger.LogDebug("Validating principal for user: {UserName}", context.Principal?.Identity?.Name);
        return base.ValidatePrincipal(context);
    }

    /// <summary>
    /// Called when the authentication ticket is signing in.
    /// </summary>
    public override Task SigningIn(CookieSigningInContext context)
    {
        _logger.LogDebug("User signing in: {UserName}", context.Principal?.Identity?.Name);
        return base.SigningIn(context);
    }

    /// <summary>
    /// Called when the authentication ticket is signing out.
    /// </summary>
    public override Task SigningOut(CookieSigningOutContext context)
    {
        _logger.LogDebug("User signing out");
        return base.SigningOut(context);
    }
}
