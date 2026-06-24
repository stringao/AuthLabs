using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;
using AuthLabs.Resource.Authorization;
using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Tests;

public class ResourceAuthorizationTests
{
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

    [Fact]
    public async Task Owner_Can_Edit_Document()
    {
        // Arrange
        var userId = "admin-id";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 1,
            Title = "Test",
            OwnerId = "admin-id",
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
    public async Task User_With_Permission_Can_Edit_Document()
    {
        // Arrange
        var userId = "manager-id";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 2,
            Title = "Test",
            OwnerId = "admin-id",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "manager-id", CanEdit = true, CanDelete = false }
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
    public async Task User_Without_Permission_Cannot_Edit_Document()
    {
        // Arrange
        var userId = "guest-id";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 1,
            Title = "Test",
            OwnerId = "admin-id",
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
    public async Task User_With_Delete_Permission_Can_Delete_Document()
    {
        // Arrange
        var userId = "admin-id";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 4,
            Title = "Test",
            OwnerId = "guest-id",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "admin-id", CanEdit = true, CanDelete = true }
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
    public async Task Admin_Can_Edit_Any_Document_With_Permission()
    {
        // Arrange - admin has permission on doc 3
        var userId = "admin-id";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 3,
            Title = "Projeto Feature X",
            OwnerId = "user-id",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "admin-id", CanEdit = true, CanDelete = true }
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
    public async Task User_Cannot_Delete_Without_Permission()
    {
        // Arrange
        var userId = "manager-id";
        var handler = CreateHandler(userId);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var document = new Document
        {
            Id = 1,
            Title = "Test",
            OwnerId = "admin-id",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "manager-id", CanEdit = true, CanDelete = false }
            }
        };

        var requirement = new DocumentOperationRequirement("Delete");
        var context = CreateContext(claimsPrincipal, document, requirement);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}

// Simple mock implementation instead of Moq
public class MockHttpContextAccessor : IHttpContextAccessor
{
    private readonly DefaultHttpContext _context;

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
