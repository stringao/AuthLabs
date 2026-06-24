using AuthLabs.Rbac.Controllers;
using AuthLabs.Rbac.Services;
using AuthLabs.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuthLabs.Rbac.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IRoleService> _roleServiceMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _controller = new AuthController(_roleServiceMock.Object, _authServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithUserInfo()
    {
        // Arrange
        var request = new LoginRequest("admin@authlabs.com", "ValidPassword123");
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };
        var roles = new List<string> { "Admin", "User" };

        _roleServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _authServiceMock
            .Setup(x => x.PasswordSignInAsync(user, request.Password, true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _roleServiceMock
            .Setup(x => x.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@authlabs.com", "AnyPassword");

        _roleServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("admin@authlabs.com", "WrongPassword");
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };

        _roleServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _authServiceMock
            .Setup(x => x.PasswordSignInAsync(user, request.Password, true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithLockedOutUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("admin@authlabs.com", "AnyPassword");
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };

        _roleServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _authServiceMock
            .Setup(x => x.PasswordSignInAsync(user, request.Password, true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_ShouldSignOutAndReturnOk()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _authServiceMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnUserRoles_WhenSuccessful()
    {
        // Arrange
        var request = new LoginRequest("admin@authlabs.com", "ValidPassword123");
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };
        var roles = new List<string> { "Admin" };

        _roleServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _authServiceMock
            .Setup(x => x.PasswordSignInAsync(user, request.Password, true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _roleServiceMock
            .Setup(x => x.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
