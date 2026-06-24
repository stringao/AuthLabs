using System.Security.Claims;
using AuthLabs.Jwt.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthLabs.Jwt.Tests.Controllers;

public class ProtectedControllerTests
{
    private readonly ProtectedController _protectedController;

    public ProtectedControllerTests()
    {
        _protectedController = new ProtectedController();
    }

    [Fact]
    public void Get_ReturnsUserInfoAndClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _protectedController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = _protectedController.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void Get_WithNoUser_ReturnsNullUserName()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _protectedController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = _protectedController.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetAdminOnly_ReturnsOkForAdminUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "adminuser"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _protectedController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = _protectedController.GetAdminOnly();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
