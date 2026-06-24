using System.Security.Claims;
using AuthLabs.Claims.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AuthLabs.Claims.Tests.Authorization;

/// <summary>
/// Testes para CustomClaimHandler -AuthorizationHandler de claims customizados.
/// </summary>
public class ClaimsRequirementHandlerTests
{
    private readonly CustomClaimHandler _handler;

    public ClaimsRequirementHandlerTests()
    {
        _handler = new CustomClaimHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMatchingClaim_Succeeds()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Document:Edit", "true");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Document:Edit", "true")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNonMatchingClaim_DoesNotSucceed()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Document:Edit", "true");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Document:Edit", "false")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMultipleClaims_MatchesCorrectOne()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Subscription:Tier", "Premium");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Document:Edit", "true"),
            new Claim("Subscription:Tier", "Premium"),
            new Claim("User:Manage", "false")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNoMatchingClaimType_DoesNotSucceed()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("User:Manage", "true");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Document:Edit", "true"),
            new Claim("Subscription:Tier", "Basic")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithEmptyClaims_DoesNotSucceed()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Document:Edit", "true");
        var user = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_CaseSensitiveValue_DoesNotMatch()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Subscription:Tier", "premium");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Subscription:Tier", "Premium")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
