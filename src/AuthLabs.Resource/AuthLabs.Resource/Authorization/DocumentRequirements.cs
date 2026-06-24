using Microsoft.AspNetCore.Authorization;

namespace AuthLabs.Resource.Authorization;

public class DocumentOperationRequirement : IAuthorizationRequirement
{
    public string Operation { get; }

    public DocumentOperationRequirement(string operation)
    {
        Operation = operation;
    }
}
