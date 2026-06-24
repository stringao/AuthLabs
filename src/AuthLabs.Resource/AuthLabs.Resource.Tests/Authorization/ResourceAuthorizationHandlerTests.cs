using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;
using AuthLabs.Resource.Authorization;
using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Tests.Authorization;

public class ResourceAuthorizationHandlerTests
{
    private DocumentAuthorizationHandler CreateHandler()
    {
        var httpContextAccessor = new MockHttpContextAccessor();
        return new DocumentAuthorizationHandler(httpContextAccessor);
    }

    private DocumentAuthorizationHandler CreateHandler(string userId)
    {
        var httpContextAccessor = new MockHttpContextAccessor(userId);
        return new DocumentAuthorizationHandler(httpContextAccessor);
    }

    private AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, Document document, IAuthorizationRequirement requirement)
    {
        return new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            document);
    }

    #region Owner Access Tests

    [Fact]
    public async Task HandleRequirementAsync_AsOwner_AlwaysSucceeds()
    {
        // Arrange
        var ownerId = "owner-user-id";
        var handler = CreateHandler(ownerId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ownerId)
        }));

        var document = new Document
        {
            Id = 1,
            Title = "Owner Test Document",
            OwnerId = ownerId,
            Permissions = new List<DocumentPermission>()
        };

        var requirement = new DocumentOperationRequirement("Edit");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_AsOwner_CanDelete()
    {
        // Arrange
        var ownerId = "owner-user-id";
        var handler = CreateHandler(ownerId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ownerId)
        }));

        var document = new Document
        {
            Id = 2,
            Title = "Owner Delete Test",
            OwnerId = ownerId,
            Permissions = new List<DocumentPermission>()
        };

        var requirement = new DocumentOperationRequirement("Delete");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_OwnerDoesNotNeedExplicitPermissions()
    {
        // Arrange
        var ownerId = "another-owner";
        var handler = CreateHandler(ownerId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ownerId)
        }));

        var document = new Document
        {
            Id = 3,
            Title = "Document Without Permissions",
            OwnerId = ownerId,
            Permissions = new List<DocumentPermission>()
        };

        var editRequirement = new DocumentOperationRequirement("Edit");
        var deleteRequirement = new DocumentOperationRequirement("Delete");

        // Act - Test Edit
        var editContext = CreateContext(claimsPrincipal, document, editRequirement);
        await handler.HandleAsync(editContext);

        // Reset and test Delete
        var deleteContext = CreateContext(claimsPrincipal, document, deleteRequirement);
        await handler.HandleAsync(deleteContext);

        // Assert
        Assert.True(editContext.HasSucceeded);
        Assert.True(deleteContext.HasSucceeded);
    }

    #endregion

    #region Permission Access Tests

    [Fact]
    public async Task HandleRequirementAsync_WithEditPermission_Succeeds()
    {
        // Arrange
        var userId = "editor-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 10,
            Title = "Permission Test Document",
            OwnerId = "different-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = true, CanDelete = false }
            }
        };

        var requirement = new DocumentOperationRequirement("Edit");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithDeletePermission_Succeeds()
    {
        // Arrange
        var userId = "deleter-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 11,
            Title = "Delete Permission Test",
            OwnerId = "different-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = false, CanDelete = true }
            }
        };

        var requirement = new DocumentOperationRequirement("Delete");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithBothPermissions_SucceedsForBothOperations()
    {
        // Arrange
        var userId = "privileged-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 12,
            Title = "Full Permission Document",
            OwnerId = "another-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = true, CanDelete = true }
            }
        };

        var editRequirement = new DocumentOperationRequirement("Edit");
        var deleteRequirement = new DocumentOperationRequirement("Delete");

        // Act
        var editContext = CreateContext(claimsPrincipal, document, editRequirement);
        await handler.HandleAsync(editContext);

        var deleteContext = CreateContext(claimsPrincipal, document, deleteRequirement);
        await handler.HandleAsync(deleteContext);

        // Assert
        Assert.True(editContext.HasSucceeded);
        Assert.True(deleteContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_CannotUseEditPermissionForDelete()
    {
        // Arrange
        var userId = "edit-only-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 13,
            Title = "Edit Only Document",
            OwnerId = "document-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = true, CanDelete = false }
            }
        };

        var deleteRequirement = new DocumentOperationRequirement("Delete");
        var context = CreateContext(claimsPrincipal, document, deleteRequirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_CannotUseDeletePermissionForEdit()
    {
        // Arrange
        var userId = "delete-only-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 14,
            Title = "Delete Only Document",
            OwnerId = "document-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = false, CanDelete = true }
            }
        };

        var editRequirement = new DocumentOperationRequirement("Edit");
        var context = CreateContext(claimsPrincipal, document, editRequirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    #endregion

    #region No Access Tests

    [Fact]
    public async Task HandleRequirementAsync_WithNoPermission_DoesNotSucceed()
    {
        // Arrange
        var userId = "no-access-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 20,
            Title = "Restricted Document",
            OwnerId = "someone-else",
            Permissions = new List<DocumentPermission>()
        };

        var requirement = new DocumentOperationRequirement("Edit");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserNotInPermissions_DoesNotSucceed()
    {
        // Arrange
        var userId = "random-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 21,
            Title = "Document With Other Permissions",
            OwnerId = "document-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "other-user", CanEdit = true, CanDelete = true }
            }
        };

        var requirement = new DocumentOperationRequirement("Edit");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserPermissionSetToFalse_DoesNotSucceed()
    {
        // Arrange
        var userId = "explicit-no-edit";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 22,
            Title = "Document With Explicit False Permissions",
            OwnerId = "document-owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = false, CanDelete = false }
            }
        };

        var editRequirement = new DocumentOperationRequirement("Edit");
        var deleteRequirement = new DocumentOperationRequirement("Delete");

        // Act
        var editContext = CreateContext(claimsPrincipal, document, editRequirement);
        await handler.HandleAsync(editContext);

        var deleteContext = CreateContext(claimsPrincipal, document, deleteRequirement);
        await handler.HandleAsync(deleteContext);

        // Assert
        Assert.False(editContext.HasSucceeded);
        Assert.False(deleteContext.HasSucceeded);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task HandleRequirementAsync_OperationIsCaseSensitive()
    {
        // Arrange
        var userId = "case-test-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 30,
            Title = "Case Sensitivity Test",
            OwnerId = "owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = true, CanDelete = true }
            }
        };

        // Test lowercase 'edit' - should NOT match 'Edit' requirement
        var lowercaseRequirement = new DocumentOperationRequirement("edit");
        var context = CreateContext(claimsPrincipal, document, lowercaseRequirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_MultiplePermissionsForSameUser_UsesFirstMatch()
    {
        // Arrange
        var userId = "multi-perm-user";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 31,
            Title = "Multiple Permissions Test",
            OwnerId = "owner",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = userId, CanEdit = false, CanDelete = true },
                new() { UserId = userId, CanEdit = true, CanDelete = false } // This one has CanEdit=true
            }
        };

        var editRequirement = new DocumentOperationRequirement("Edit");
        var context = CreateContext(claimsPrincipal, document, editRequirement);

        // Act
        await handler.HandleAsync(context);

        // Assert - FirstOrDefault returns first match, which has CanEdit=false
        Assert.False(context.HasSucceeded);
    }

    #endregion
}

// Simple mock implementation
public class MockHttpContextAccessor : IHttpContextAccessor
{
    private readonly DefaultHttpContext _context;

    public MockHttpContextAccessor() : this("default-user")
    {
    }

    public MockHttpContextAccessor(string userId)
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _context = new DefaultHttpContext { User = claimsPrincipal };
    }

    public HttpContext? HttpContext
    {
        get => _context;
        set { }
    }
}
