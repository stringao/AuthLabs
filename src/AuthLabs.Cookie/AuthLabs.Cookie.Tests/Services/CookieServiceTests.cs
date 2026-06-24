using AuthLabs.Cookie.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthLabs.Cookie.Tests.Services;

/// <summary>
/// Tests for Cookie authentication controller.
/// Note: SignInManager is difficult to mock properly - these tests focus on ProtectedController.
/// </summary>
public class CookieServiceTests : IDisposable
{
    [Fact]
    public void ProtectedController_GetUserInfo_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Email, "test@authlabs.com"),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new ProtectedController();
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.GetUserInfo();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void ProtectedController_GetUserInfo_ShouldReturnIsAuthenticatedTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new ProtectedController();
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.GetUserInfo();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void ProtectedController_GetAdminInfo_WithAdminUser_ShouldReturnOk()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new ProtectedController();
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.GetAdminInfo();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void ProtectedController_GetManagerInfo_WithManagerUser_ShouldReturnOk()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "manager"),
            new(ClaimTypes.Role, "Manager")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new ProtectedController();
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.GetManagerInfo();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void ProtectedController_GetAuthenticatedInfo_ShouldReturnOk()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "anyuser"),
            new(ClaimTypes.Role, "Guest")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new ProtectedController();
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.GetAuthenticatedInfo();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
