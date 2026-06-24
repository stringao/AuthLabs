using System.Security.Claims;
using AuthLabs.Windows.Services;

namespace AuthLabs.Windows.Tests;

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
}
