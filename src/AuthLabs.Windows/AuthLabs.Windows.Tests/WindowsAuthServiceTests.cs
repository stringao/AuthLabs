using System.Security.Claims;
using AuthLabs.Windows.Services;
using Microsoft.AspNetCore.Authentication;
using Moq;

namespace AuthLabs.Windows.Tests;

public class WindowsAuthServiceTests
{
    private readonly WindowsAuthService _service;

    public WindowsAuthServiceTests()
    {
        _service = new WindowsAuthService();
    }

    [Fact]
    public void GetCurrentUserName_ReturnsName_WhenIdentityIsPresent()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.GetCurrentUserName(principal);

        // Assert
        Assert.Equal("DOMAIN\\AdminUser", result);
    }

    [Fact]
    public void GetCurrentUserName_ReturnsNull_WhenNoIdentity()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act
        var result = _service.GetCurrentUserName(principal);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAuthenticationType_ReturnsNegotiate_WhenAuthenticated()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\User")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.GetAuthenticationType(principal);

        // Assert
        Assert.Equal("Negotiate", result);
    }

    [Fact]
    public async Task IsInAdGroupAsync_ReturnsTrue_WhenUserIsAdminAndGroupIsDomainAdmins()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "Domain Admins");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsInAdGroupAsync_ReturnsFalse_WhenUserIsAdminAndGroupIsNotInList()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "NonExistentGroup");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserAdGroupsAsync_ReturnsGroups_ForAdminUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.GetUserAdGroupsAsync(principal);
        var groupsList = result.ToList();

        // Assert
        Assert.Contains("Domain Admins", groupsList);
        Assert.Contains("Enterprise Admins", groupsList);
        Assert.Contains("Schema Admins", groupsList);
    }

    [Fact]
    public async Task GetUserAdGroupsAsync_ReturnsDomainUsers_ForRegularUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\RegularUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.GetUserAdGroupsAsync(principal);
        var groupsList = result.ToList();

        // Assert
        Assert.Contains("Domain Users", groupsList);
        Assert.Contains("Workstations", groupsList);
    }

    [Fact]
    public async Task IsInAdGroupAsync_ReturnsFalse_WhenUserNameIsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "Domain Admins");

        // Assert
        Assert.False(result);
    }
}
