using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Authorization;

public class DocumentAuthorizationHandler : AuthorizationHandler<DocumentOperationRequirement, Document>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOperationRequirement requirement,
        Document document)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Proprietário tem todas as permissões
        if (document.OwnerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Verifica permissões específicas
        var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);

        if (requirement.Operation == "Edit" && permission?.CanEdit == true)
            context.Succeed(requirement);
        else if (requirement.Operation == "Delete" && permission?.CanDelete == true)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
