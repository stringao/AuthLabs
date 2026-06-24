using System.Security.Claims;
using AuthLabs.Windows.Services;

namespace AuthLabs.Windows.Tests.Services;

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

    [Fact]
    public async Task GetUserAdGroupsAsync_ReturnsEmpty_WhenUserNameIsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.GetUserAdGroupsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task IsInAdGroupAsync_CaseInsensitive_MatchesAdminInUserName()
    {
        // Arrange - User with "admin" lowercase
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\john_admin")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "Domain Admins");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsInAdGroupAsync_GroupNameCaseInsensitive()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "domain admins");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetUserAdGroupsAsync_ReturnsGroups_ForUserWithoutAdminInName()
    {
        // Arrange - User without "Admin" in name maps to regular "User" groups
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\UnknownUser12345")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = (await _service.GetUserAdGroupsAsync(principal)).ToList();

        // Assert - Unknown users without "Admin" in name get mapped to regular User groups
        Assert.Contains("Domain Users", result);
        Assert.Contains("Workstations", result);
    }

    [Fact]
    public void GetAuthenticationType_ReturnsNull_WhenNoIdentity()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act
        var result = _service.GetAuthenticationType(principal);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsInAdGroupAsync_ReturnsTrue_ForEnterpriseAdmins()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "Enterprise Admins");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsInAdGroupAsync_ReturnsFalse_ForRegularUserInAdminGroup()
    {
        // Arrange - Regular user (no "Admin" in name)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\RegularUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.IsInAdGroupAsync(principal, "Domain Admins");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserAdGroupsAsync_ReturnsAllGroups_ForAdminUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\AdminUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = (await _service.GetUserAdGroupsAsync(principal)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Domain Admins", result);
        Assert.Contains("Enterprise Admins", result);
        Assert.Contains("Schema Admins", result);
    }

    [Fact]
    public async Task GetUserAdGroupsAsync_ReturnsAllGroups_ForRegularUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "DOMAIN\\RegularUser")
        };
        var identity = new ClaimsIdentity(claims, "Negotiate");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = (await _service.GetUserAdGroupsAsync(principal)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Domain Users", result);
        Assert.Contains("Workstations", result);
    }
}
