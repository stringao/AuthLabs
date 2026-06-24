using AuthLabs.Cookie.Controllers;
using AuthLabs.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AuthLabs.Cookie.Tests.Controllers;

/// <summary>
/// Tests for AuthController covering login, logout, and get current user.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userManagerMock = MockUserManager();
        _signInManagerMock = MockSignInManager();

        _controller = new AuthController(_signInManagerMock.Object, _userManagerMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return userManagerMock;
    }

    private static Mock<SignInManager<User>> MockSignInManager()
    {
        var userManagerMock = MockUserManager();
        var signInManagerMock = new Mock<SignInManager<User>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);
        return signInManagerMock;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest("test@authlabs.com", "ValidPassword123");
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.Success;

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(signInResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(new { message = "Login realizado com sucesso" });
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@authlabs.com", "WrongPassword");
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.Failed;

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(signInResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Credenciais inválidas" });
    }

    [Fact]
    public async Task Login_WhenLockedOut_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@authlabs.com", "WrongPassword");
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.LockedOut;

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(signInResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WhenRequiresTwoFactor_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@authlabs.com", "WrongPassword");
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired;

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(signInResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_IsNotAllowed_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@authlabs.com", "WrongPassword");
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.NotAllowed;

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(signInResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_CallsSignOut_ReturnsOk()
    {
        // Arrange
        _signInManagerMock
            .Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(new { message = "Logout realizado com sucesso" });

        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public void AccessDenied_ReturnsForbid()
    {
        // Act
        var result = _controller.AccessDenied();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Login_CallsPasswordSignInAsync_WithCorrectParameters()
    {
        // Arrange
        var request = new LoginRequest("user@test.com", "Password123!");
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.Success;

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(signInResult)
            .Verifiable();

        // Act
        await _controller.Login(request);

        // Assert
        _signInManagerMock.Verify(x => x.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false), Times.Once);
    }

    [Fact]
    public async Task Logout_CallsSignOutAsync_Once()
    {
        // Arrange
        _signInManagerMock
            .Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _controller.Logout();

        // Assert
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }
}

/// <summary>
/// Tests for LoginRequest record.
/// </summary>
public class LoginRequestTests
{
    [Fact]
    public void LoginRequest_CreatesWithEmailAndPassword()
    {
        // Arrange & Act
        var request = new LoginRequest("test@authlabs.com", "Password123");

        // Assert
        request.Email.Should().Be("test@authlabs.com");
        request.Password.Should().Be("Password123");
    }

    [Fact]
    public void LoginRequest_SupportsDeconstruction()
    {
        // Arrange
        var request = new LoginRequest("user@test.com", "pass123");

        // Act
        var (email, password) = request;

        // Assert
        email.Should().Be("user@test.com");
        password.Should().Be("pass123");
    }
}
