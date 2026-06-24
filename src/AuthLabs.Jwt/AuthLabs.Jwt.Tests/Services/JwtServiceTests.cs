using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthLabs.Jwt.Services;
using AuthLabs.Shared.Models;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace AuthLabs.Jwt.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        _jwtService = new JwtService(_jwtSettings);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserName = "testuser"
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Should().NotBeNull();
        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainUserClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Email = "user@example.com",
            UserName = "username"
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Subject.Should().Be("42");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_WithAdditionalClaims_ShouldIncludeThem()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserName = "testuser"
        };
        var additionalClaims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new("custom_claim", "custom_value")
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user, additionalClaims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwtToken.Claims.Should().Contain(c => c.Type == "custom_claim" && c.Value == "custom_value");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueToken()
    {
        // Act
        var refreshToken1 = _jwtService.GenerateRefreshToken();
        var refreshToken2 = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken1.Should().NotBeNullOrEmpty();
        refreshToken2.Should().NotBeNullOrEmpty();
        refreshToken1.Should().NotBe(refreshToken2);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserName = "testuser"
        };
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var expiredSettings = new JwtSettings
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = -1 // Already expired
        };
        var expiredService = new JwtService(expiredSettings);
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserName = "testuser"
        };
        var token = expiredService.GenerateAccessToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongSecretKey_ShouldReturnNull()
    {
        // Arrange
        var differentSettings = new JwtSettings
        {
            SecretKey = "CompletelyDifferentSecretKeyThatWontMatch!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15
        };
        var differentService = new JwtService(differentSettings);
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserName = "testuser"
        };
        var token = differentService.GenerateAccessToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }
}