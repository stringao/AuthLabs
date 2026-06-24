using AuthLabs.Jwt.Services;
using AuthLabs.Shared.Data;
using AuthLabs.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SystemClaim = System.Security.Claims.Claim;
using IEnumerableClaim = System.Collections.Generic.IEnumerable<System.Security.Claims.Claim>;

namespace AuthLabs.Jwt.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        // Setup UserManager mock
        _userManagerMock = CreateUserManagerMock();

        // Setup JwtService mock
        _jwtServiceMock = new Mock<IJwtService>();
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerableClaim>()))
            .Returns("mock-access-token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("mock-refresh-token");

        // Create AuthService
        _authService = new AuthService(_userManagerMock.Object, _jwtServiceMock.Object, _dbContext);
    }

    private Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return userManager;
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", UserName = "testuser" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Password123!"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _authService.LoginAsync("test@example.com", "Password123!");

        // Assert
        result.Should().NotBeNull();
        result!.Value.accessToken.Should().Be("mock-access-token");
        result.Value.refreshToken.Should().Be("mock-refresh-token");

        var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync();
        storedToken.Should().NotBeNull();
        storedToken!.UserId.Should().Be(1);
        storedToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", UserName = "testuser" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "WrongPassword!"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync("test@example.com", "WrongPassword!");

        // Assert
        result.Should().BeNull();
        _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerableClaim>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ShouldReturnNull()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync("nonexistent@example.com", "Password123!");

        // Assert
        result.Should().BeNull();
        _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerableClaim>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", UserName = "testuser" };
        var refreshToken = new RefreshToken
        {
            Token = "valid-refresh-token",
            UserId = 1,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _authService.RefreshTokenAsync("valid-refresh-token");

        // Assert
        result.Should().NotBeNull();
        result!.Value.accessToken.Should().Be("mock-access-token");
        result.Value.refreshToken.Should().Be("mock-refresh-token");

        // Old token should be revoked
        var oldToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "valid-refresh-token");
        oldToken!.IsRevoked.Should().BeTrue();

        // New token should be stored
        var newToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "mock-refresh-token");
        newToken.Should().NotBeNull();
        newToken!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", UserName = "testuser" };
        var expiredToken = new RefreshToken
        {
            Token = "expired-token",
            UserId = 1,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.RefreshTokenAsync("expired-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", UserName = "testuser" };
        var revokedToken = new RefreshToken
        {
            Token = "revoked-token",
            UserId = 1,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true // Already revoked
        };
        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.RefreshTokenAsync("revoked-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNonexistentToken_ShouldReturnNull()
    {
        // Act
        var result = await _authService.RefreshTokenAsync("nonexistent-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Token = "token-to-revoke",
            UserId = 1,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.RevokeRefreshTokenAsync("token-to-revoke");

        // Assert
        result.Should().BeTrue();
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "token-to-revoke");
        token!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonexistentToken_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.RevokeRefreshTokenAsync("nonexistent-token");

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}