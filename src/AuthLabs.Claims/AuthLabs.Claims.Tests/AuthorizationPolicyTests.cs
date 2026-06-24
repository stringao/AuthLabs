using AuthLabs.Claims.Services;
using Xunit;

namespace AuthLabs.Claims.Tests;

/// <summary>
/// Testes para ClaimsService.
/// </summary>
public class ClaimsServiceTests
{
    private readonly ClaimsService _service;

    public ClaimsServiceTests()
    {
        _service = new ClaimsService();
    }

    [Fact]
    public void GetUserClaims_Admin_DeveTerTodosClaims()
    {
        // Act
        var claims = _service.GetUserClaims("admin@authlabs.com");

        // Assert
        Assert.Equal("true", claims["Document:Edit"]);
        Assert.Equal("true", claims["Document:Delete"]);
        Assert.Equal("true", claims["User:Manage"]);
        Assert.Equal("Premium", claims["Subscription:Tier"]);
    }

    [Fact]
    public void GetUserClaims_Manager_DeveTerEditESubscription()
    {
        // Act
        var claims = _service.GetUserClaims("manager@authlabs.com");

        // Assert
        Assert.Equal("true", claims["Document:Edit"]);
        Assert.Equal("Standard", claims["Subscription:Tier"]);
        Assert.False(claims.ContainsKey("Document:Delete"));
        Assert.False(claims.ContainsKey("User:Manage"));
    }

    [Fact]
    public void GetUserClaims_User_DeveTerApenasEditEBasic()
    {
        // Act
        var claims = _service.GetUserClaims("user@authlabs.com");

        // Assert
        Assert.Equal("true", claims["Document:Edit"]);
        Assert.Equal("Basic", claims["Subscription:Tier"]);
        Assert.False(claims.ContainsKey("Document:Delete"));
        Assert.False(claims.ContainsKey("User:Manage"));
    }

    [Fact]
    public void GetUserClaims_Guest_DeveTerClaimsVazios()
    {
        // Act
        var claims = _service.GetUserClaims("guest@authlabs.com");

        // Assert
        Assert.Empty(claims);
    }

    [Fact]
    public void GetUserClaims_UsuarioInexistente_DeveRetornarDicionarioVazio()
    {
        // Act
        var claims = _service.GetUserClaims("unknown@authlabs.com");

        // Assert
        Assert.Empty(claims);
    }
}
