using System.Security.Claims;
using AuthLabs.Windows.Services;

namespace AuthLabs.Windows.Tests.Services;

public class WindowsClaimsTransformerTests
{
    private readonly WindowsClaimsTransformer _transformer;

    public WindowsClaimsTransformerTests()
    {
        _transformer = new WindowsClaimsTransformer();
    }

    [Fact]
    public async Task TransformAsync_AddsAdminRole_WhenUserNameContainsAdmin()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Assert
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task TransformAsync_AddsUserRole_WhenUserNameDoesNotContainAdmin()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\RegularUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Assert
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task TransformAsync_AddsWindowsAuthClaim()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\SomeUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var windowsAuthClaim = result.FindFirst("WindowsAuth");

        // Assert
        Assert.NotNull(windowsAuthClaim);
        Assert.Equal("true", windowsAuthClaim!.Value);
    }

    [Fact]
    public async Task TransformAsync_ReturnsOriginalPrincipal_WhenIdentityIsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act
        var result = await _transformer.TransformAsync(principal);

        // Assert
        Assert.Same(principal, result);
    }

    [Fact]
    public async Task TransformAsync_AddsCorrectRole_ForAdminWithMixedCase()
    {
        // Arrange - admin with mixed case
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\ADMINUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Assert
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task TransformAsync_AddsUserRole_ForUserWithAdminInNameButNotAdminUser()
    {
        // Arrange - user with "Admin" in name but is a regular user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminAssistant")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Assert - Should be Admin because name contains "Admin"
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task TransformAsync_PreservesExistingClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser"),
            new Claim("CustomClaim", "CustomValue")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);

        // Assert - Original custom claim should still exist
        Assert.NotNull(result.FindFirst("CustomClaim"));
        Assert.Equal("CustomValue", result.FindFirst("CustomClaim")!.Value);
    }

    [Fact]
    public async Task TransformAsync_AddsWindowsAuthClaim_WithCorrectValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\User")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);

        // Assert
        var windowsAuthClaims = result.FindAll("WindowsAuth");
        Assert.Single(windowsAuthClaims);
        Assert.Equal("true", windowsAuthClaims.First().Value);
    }

    [Fact]
    public async Task TransformAsync_AddsBothRoleAndWindowsAuthClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);

        // Assert
        Assert.NotNull(result.FindFirst(ClaimTypes.Role));
        Assert.NotNull(result.FindFirst("WindowsAuth"));
    }

    [Fact]
    public async Task TransformAsync_AddsRoleClaim_ToExistingIdentity()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\RegularUser"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roleClaims = result.FindAll(ClaimTypes.Role);

        // Assert
        Assert.Single(roleClaims);
        Assert.Equal("User", roleClaims.First().Value);
    }

    [Fact]
    public async Task TransformAsync_AdminUser_GetsMultipleRoles()
    {
        // Arrange - Admin gets Admin role
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roleClaims = result.FindAll(ClaimTypes.Role);

        // Assert
        Assert.Single(roleClaims);
        Assert.Equal("Admin", roleClaims.First().Value);
    }

    [Fact]
    public async Task TransformAsync_RegularUser_GetsUserRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\StandardUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roleClaims = result.FindAll(ClaimTypes.Role);

        // Assert
        Assert.Single(roleClaims);
        Assert.Equal("User", roleClaims.First().Value);
    }

    [Fact]
    public async Task TransformAsync_HandlesNullName_Gracefully()
    {
        // Arrange - Create an identity where Name is null by using a custom identity
        // that overrides Name to return null
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        identity.AddClaim(new Claim(ClaimTypes.Name, "")); // Ensure name is empty
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert - Should not throw and should return User role
        var result = await _transformer.TransformAsync(principal);
        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task TransformAsync_HandlesEmptyName_ReturnsUserRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformer.TransformAsync(principal);
        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Assert - Empty name should get User role
        Assert.Contains("User", roles);
    }
}
