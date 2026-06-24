using Microsoft.AspNetCore.Authorization;

namespace AuthLabs.Claims.Authorization;

/// <summary>
/// Handler que verifica se o usuário possui o claim requerido.
/// </summary>
public class CustomClaimHandler : AuthorizationHandler<CustomClaimRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomClaimRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == requirement.ClaimType && c.Value == requirement.ClaimValue))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
