using System.Security.Claims;
using AuthLabs.Claims.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AuthLabs.Claims.Tests;

/// <summary>
/// Testes para CustomClaimRequirement e CustomClaimHandler.
/// </summary>
public class ClaimsRequirementTests
{
    private readonly CustomClaimHandler _handler;

    public ClaimsRequirementTests()
    {
        _handler = new CustomClaimHandler();
    }

    [Fact]
    public async Task Handler_DeveFalhar_QuandoClaimNaoExistir()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Document:Edit", "true");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Subscription:Tier", "Basic")
        }));

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
    public async Task Handler_DeveSucceedir_QuandoClaimCorresponder()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Document:Edit", "true");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Document:Edit", "true"),
            new Claim("Subscription:Tier", "Premium")
        }));

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
    public async Task Handler_DeveFalhar_QuandoValorDaClaimNaoCorresponder()
    {
        // Arrange
        var requirement = new CustomClaimRequirement("Subscription:Tier", "Premium");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Subscription:Tier", "Basic")
        }));

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
    public void CustomClaimRequirement_DeveTerPropertiesCorretas()
    {
        // Arrange & Act
        var requirement = new CustomClaimRequirement("Document:Delete", "true");

        // Assert
        Assert.Equal("Document:Delete", requirement.ClaimType);
        Assert.Equal("true", requirement.ClaimValue);
    }

    [Theory]
    [InlineData("Document:Edit", "true")]
    [InlineData("User:Manage", "true")]
    [InlineData("Subscription:Tier", "Premium")]
    public void CustomClaimRequirement_DeveAceitarDiferentesClaims(string claimType, string claimValue)
    {
        // Arrange & Act
        var requirement = new CustomClaimRequirement(claimType, claimValue);

        // Assert
        Assert.Equal(claimType, requirement.ClaimType);
        Assert.Equal(claimValue, requirement.ClaimValue);
    }
}
