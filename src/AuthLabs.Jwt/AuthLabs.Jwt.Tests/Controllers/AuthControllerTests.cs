using AuthLabs.Jwt.Controllers;
using AuthLabs.Jwt.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthLabs.Jwt.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _authController = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var loginRequest = new LoginRequest("test@example.com", "Password123!");
        var tokens = (accessToken: "valid-access-token", refreshToken: "valid-refresh-token");
        _authServiceMock.Setup(x => x.LoginAsync("test@example.com", "Password123!"))
            .ReturnsAsync(tokens);

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new
        {
            accessToken = "valid-access-token",
            refreshToken = "valid-refresh-token",
            expiresIn = 900
        });
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("test@example.com", "WrongPassword!");
        _authServiceMock.Setup(x => x.LoginAsync("test@example.com", "WrongPassword!"))
            .ReturnsAsync((ValueTuple<string, string>?)null);

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Credenciais inválidas" });
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshRequest = new RefreshRequest("valid-refresh-token");
        var tokens = (accessToken: "new-access-token", refreshToken: "new-refresh-token");
        _authServiceMock.Setup(x => x.RefreshTokenAsync("valid-refresh-token"))
            .ReturnsAsync(tokens);

        // Act
        var result = await _authController.Refresh(refreshRequest);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new
        {
            accessToken = "new-access-token",
            refreshToken = "new-refresh-token",
            expiresIn = 900
        });
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequest("invalid-refresh-token");
        _authServiceMock.Setup(x => x.RefreshTokenAsync("invalid-refresh-token"))
            .ReturnsAsync((ValueTuple<string, string>?)null);

        // Act
        var result = await _authController.Refresh(refreshRequest);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Refresh token inválido ou expirado" });
    }

    [Fact]
    public async Task Logout_CallsRevokeRefreshToken()
    {
        // Arrange
        var logoutRequest = new LogoutRequest("token-to-revoke");
        _authServiceMock.Setup(x => x.RevokeRefreshTokenAsync("token-to-revoke"))
            .ReturnsAsync(true);

        // Act
        var result = await _authController.Logout(logoutRequest);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Logout realizado com sucesso" });
        _authServiceMock.Verify(x => x.RevokeRefreshTokenAsync("token-to-revoke"), Times.Once);
    }

    [Fact]
    public async Task Logout_WhenTokenNotFound_ReturnsOkRegardless()
    {
        // Arrange
        var logoutRequest = new LogoutRequest("nonexistent-token");
        _authServiceMock.Setup(x => x.RevokeRefreshTokenAsync("nonexistent-token"))
            .ReturnsAsync(false);

        // Act
        var result = await _authController.Logout(logoutRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _authServiceMock.Verify(x => x.RevokeRefreshTokenAsync("nonexistent-token"), Times.Once);
    }
}
