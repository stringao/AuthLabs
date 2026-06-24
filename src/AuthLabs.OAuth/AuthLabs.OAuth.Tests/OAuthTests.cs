using System.Security.Claims;
using AuthLabs.OAuth.Infrastructure;
using AuthLabs.OAuth.Controllers;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AuthLabs.OAuth.Tests;

public class OAuthTests
{
    [Fact]
    public void OAuthProviderConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new OAuthProviderConfig();

        // Assert
        Assert.Equal(string.Empty, config.ProviderName);
        Assert.Equal(string.Empty, config.ClientId);
        Assert.Equal(string.Empty, config.ClientSecret);
        Assert.Equal(string.Empty, config.AuthorizationEndpoint);
        Assert.Equal(string.Empty, config.TokenEndpoint);
        Assert.Equal(string.Empty, config.UserInfoEndpoint);
        Assert.Equal(string.Empty, config.CallbackPath);
    }

    [Fact]
    public void OAuthSettings_CanStoreMultipleProviders()
    {
        // Arrange
        var settings = new OAuthSettings();

        // Act
        settings.Providers["Google"] = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "test-client-id",
            ClientSecret = "test-secret"
        };
        settings.Providers["GitHub"] = new OAuthProviderConfig
        {
            ProviderName = "GitHub",
            ClientId = "github-client-id",
            ClientSecret = "github-secret"
        };

        // Assert
        Assert.Equal(2, settings.Providers.Count);
        Assert.Equal("Google", settings.Providers["Google"].ProviderName);
        Assert.Equal("GitHub", settings.Providers["GitHub"].ProviderName);
    }

    [Fact]
    public void User_CanBeCreatedWithExternalLogin()
    {
        // Arrange & Act
        var user = new User
        {
            UserName = "testuser",
            Email = "testuser@example.com"
        };

        // Assert
        Assert.Equal("testuser", user.UserName);
        Assert.Equal("testuser@example.com", user.Email);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ProtectedController_PublicEndpoint_ReturnsOk()
    {
        // Arrange
        var controller = new ProtectedController();

        // Act
        var result = controller.Public();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void ProtectedController_SecureEndpoint_ReturnsOk_WithAuthenticatedUser()
    {
        // Arrange
        var controller = new ProtectedController();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.AuthenticationMethod, "Google")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = controller.Secure();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void ProtectedController_ProfileEndpoint_ReturnsUserInfo()
    {
        // Arrange
        var controller = new ProtectedController();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.AuthenticationMethod, "Google")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = controller.Profile();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void OAuthProviderConfig_CanStoreEndpoints()
    {
        // Arrange
        var config = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "client-123",
            ClientSecret = "secret-456",
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            UserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo",
            CallbackPath = "/signin-google"
        };

        // Assert
        Assert.Equal("Google", config.ProviderName);
        Assert.Equal("https://accounts.google.com/o/oauth2/v2/auth", config.AuthorizationEndpoint);
        Assert.Equal("https://oauth2.googleapis.com/token", config.TokenEndpoint);
        Assert.Equal("https://www.googleapis.com/oauth2/v3/userinfo", config.UserInfoEndpoint);
        Assert.Equal("/signin-google", config.CallbackPath);
    }
}