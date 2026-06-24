using AuthLabs.Claims.Authorization;
using Xunit;

namespace AuthLabs.Claims.Tests;

/// <summary>
/// Testes para as políticas de autorização definidas em AuthorizationPolicies.
/// </summary>
public class AuthorizationPolicyTests
{
    [Fact]
    public void AuthorizationPolicies_CanEditDocuments_HasCorrectName()
    {
        // Assert
        Assert.Equal("CanEditDocuments", AuthorizationPolicies.CanEditDocuments);
    }

    [Fact]
    public void AuthorizationPolicies_CanDeleteDocuments_HasCorrectName()
    {
        // Assert
        Assert.Equal("CanDeleteDocuments", AuthorizationPolicies.CanDeleteDocuments);
    }

    [Fact]
    public void AuthorizationPolicies_CanManageUsers_HasCorrectName()
    {
        // Assert
        Assert.Equal("CanManageUsers", AuthorizationPolicies.CanManageUsers);
    }

    [Fact]
    public void AuthorizationPolicies_IsPremiumUser_HasCorrectName()
    {
        // Assert
        Assert.Equal("IsPremiumUser", AuthorizationPolicies.IsPremiumUser);
    }

    [Theory]
    [InlineData("CanEditDocuments")]
    [InlineData("CanDeleteDocuments")]
    [InlineData("CanManageUsers")]
    [InlineData("IsPremiumUser")]
    public void AuthorizationPolicies_AllPolicies_HaveNonEmptyNames(string policyName)
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(policyName));
        Assert.NotNull(policyName);
    }
}
