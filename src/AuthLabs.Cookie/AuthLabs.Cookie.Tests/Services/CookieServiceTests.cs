using AuthLabs.Cookie.Controllers;
using AuthLabs.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AuthLabs.Cookie.Tests.Services;

public class CookieServiceTests : IDisposable
{
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly AuthController _authController;

    public CookieServiceTests()
    {
        // Setup UserManager mock
        _userManagerMock = CreateUserManagerMock();

        // Setup SignInManager mock
        _signInManagerMock = CreateSignInManagerMock();

        // Create controller
        _authController = new AuthController(_signInManagerMock.Object, _userManagerMock.Object);
    }

    private Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return userManager;
    }

    private Mock<SignInManager<User>> CreateSignInManagerMock()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = new Mock<SignInManager<User>>(
            userManagerMock: userManager.Object,
            contextAccessor: new Mock<IHttpContextAccessor>().Object,
            claimsFactory: new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            optionsAccessor: new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Object,
            loggerAccessor: new Mock<ILogger<SignInManager<User>>>().Object,
            schemes: new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            userConfirmation: new Mock<Microsoft.AspNetCore.Identity.IUserConfirmation<User>>().Object);
        return signInManager;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("admin@authlabs.com"))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                "admin@authlabs.com",
                "Admin123!",
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _authController.Login(new LoginRequest("admin@authlabs.com", "Admin123!"));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("admin@authlabs.com"))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                "admin@authlabs.com",
                "WrongPassword!",
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _authController.Login(new LoginRequest("admin@authlabs.com", "WrongPassword!"));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByEmailAsync("nonexistent@authlabs.com"))
            .ReturnsAsync((User?)null);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                "nonexistent@authlabs.com",
                "AnyPassword!",
                isPersistent: true,
                lockoutOnFailure: false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _authController.Login(new LoginRequest("nonexistent@authlabs.com", "AnyPassword!"));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WithAuthenticatedUser_ShouldReturnUserInfo()
    {
        // Arrange
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Email, "admin@authlabs.com"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _signInManagerMock.Setup(x => x.UserManager.GetUserAsync(principal))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.UserManager.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        // Setup HttpContext with authenticated user
        var httpContext = new DefaultHttpContext { User = principal };
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _authController.GetCurrentUser();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_ShouldCallSignOutAsync()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Setup HttpContext with authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _authController.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WithUnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        _signInManagerMock.Setup(x => x.UserManager.GetUserAsync(principal))
            .ReturnsAsync((User?)null);

        // Setup HttpContext with unauthenticated user
        var httpContext = new DefaultHttpContext { User = principal };
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _authController.GetCurrentUser();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
