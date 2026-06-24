using AuthLabs.Rbac.Services;
using AuthLabs.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AuthLabs.Rbac.Tests.Services;

public class RoleServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly RoleService _roleService;

    public RoleServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _roleService = new RoleService(_userManagerMock.Object);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var email = "admin@authlabs.com";
        var user = new User { Id = 1, Email = email, UserName = "admin" };
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await _roleService.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@authlabs.com";
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _roleService.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnUserRoles()
    {
        // Arrange
        var user = new User { Id = 1, Email = "admin@authlabs.com", UserName = "admin" };
        var roles = new List<string> { "Admin", "User" };
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _roleService.GetUserRolesAsync(user);

        // Assert
        result.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task GetUserRolesAsync_WhenUserHasNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var user = new User { Id = 1, Email = "newuser@authlabs.com", UserName = "newuser" };
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _roleService.GetUserRolesAsync(user);

        // Assert
        result.Should().BeEmpty();
    }
}
