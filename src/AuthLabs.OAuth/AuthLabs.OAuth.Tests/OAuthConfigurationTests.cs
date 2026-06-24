using AuthLabs.OAuth.Infrastructure;
using Xunit;

namespace AuthLabs.OAuth.Tests;

public class OAuthConfigurationTests
{
    [Fact]
    public void OAuthProviderConfig_DefaultValues_AreEmptyStrings()
    {
        // Arrange & Act
        var config = new OAuthProviderConfig();

        // Assert
        Assert.Equal(string.Empty, config.ProviderName);
        Assert.Equal(string.Empty, config.ClientId);
        Assert.Equal(string.Empty, config.ClientSecret);
        Assert.Equal(string.Empty, config.AuthorizationEndpoint);
        Assert.Equal(string.Empty, config.TokenEndpoint);
        Assert.Equal(string.Empty, config.UserInfoEndpoint);
        Assert.Equal(string.Empty, config.CallbackPath);
    }

    [Fact]
    public void OAuthProviderConfig_CanSetAllProperties()
    {
        // Arrange & Act
        var config = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "123456789-abcdef.apps.googleusercontent.com",
            ClientSecret = "super-secret-key",
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            UserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo",
            CallbackPath = "/signin-google"
        };

        // Assert
        Assert.Equal("Google", config.ProviderName);
        Assert.Equal("123456789-abcdef.apps.googleusercontent.com", config.ClientId);
        Assert.Equal("super-secret-key", config.ClientSecret);
        Assert.Equal("https://accounts.google.com/o/oauth2/v2/auth", config.AuthorizationEndpoint);
        Assert.Equal("https://oauth2.googleapis.com/token", config.TokenEndpoint);
        Assert.Equal("https://www.googleapis.com/oauth2/v3/userinfo", config.UserInfoEndpoint);
        Assert.Equal("/signin-google", config.CallbackPath);
    }

    [Fact]
    public void OAuthSettings_ProvidersDictionary_IsInitializedEmpty()
    {
        // Arrange & Act
        var settings = new OAuthSettings();

        // Assert
        Assert.NotNull(settings.Providers);
        Assert.Empty(settings.Providers);
    }

    [Fact]
    public void OAuthSettings_CanAddSingleProvider()
    {
        // Arrange
        var settings = new OAuthSettings();

        // Act
        settings.Providers["Google"] = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "google-client-id",
            ClientSecret = "google-secret"
        };

        // Assert
        Assert.Single(settings.Providers);
        Assert.Equal("Google", settings.Providers["Google"].ProviderName);
    }

    [Fact]
    public void OAuthSettings_CanStoreMultipleProviders()
    {
        // Arrange
        var settings = new OAuthSettings();

        // Act
        settings.Providers["Google"] = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "google-client-id",
            ClientSecret = "google-secret"
        };
        settings.Providers["GitHub"] = new OAuthProviderConfig
        {
            ProviderName = "GitHub",
            ClientId = "github-client-id",
            ClientSecret = "github-secret"
        };
        settings.Providers["Microsoft"] = new OAuthProviderConfig
        {
            ProviderName = "Microsoft",
            ClientId = "microsoft-client-id",
            ClientSecret = "microsoft-secret"
        };

        // Assert
        Assert.Equal(3, settings.Providers.Count);
        Assert.Equal("Google", settings.Providers["Google"].ProviderName);
        Assert.Equal("GitHub", settings.Providers["GitHub"].ProviderName);
        Assert.Equal("Microsoft", settings.Providers["Microsoft"].ProviderName);
    }

    [Fact]
    public void OAuthSettings_CanUpdateExistingProvider()
    {
        // Arrange
        var settings = new OAuthSettings();
        settings.Providers["Google"] = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "old-client-id"
        };

        // Act
        settings.Providers["Google"] = new OAuthProviderConfig
        {
            ProviderName = "Google",
            ClientId = "new-client-id",
            ClientSecret = "new-secret"
        };

        // Assert
        Assert.Single(settings.Providers);
        Assert.Equal("new-client-id", settings.Providers["Google"].ClientId);
    }

    [Fact]
    public void OAuthSettings_CanRemoveProvider()
    {
        // Arrange
        var settings = new OAuthSettings();
        settings.Providers["Google"] = new OAuthProviderConfig { ProviderName = "Google" };
        settings.Providers["GitHub"] = new OAuthProviderConfig { ProviderName = "GitHub" };

        // Act
        settings.Providers.Remove("Google");

        // Assert
        Assert.Single(settings.Providers);
        Assert.False(settings.Providers.ContainsKey("Google"));
        Assert.True(settings.Providers.ContainsKey("GitHub"));
    }

    [Fact]
    public void OAuthProviderConfig_GitHubConfiguration_IsValid()
    {
        // Arrange & Act
        var config = new OAuthProviderConfig
        {
            ProviderName = "GitHub",
            ClientId = "Iv1.1234567890abcdef",
            ClientSecret = "ghp_secret123",
            AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
            TokenEndpoint = "https://github.com/login/oauth/access_token",
            UserInfoEndpoint = "https://api.github.com/user",
            CallbackPath = "/signin-github"
        };

        // Assert
        Assert.Equal("GitHub", config.ProviderName);
        Assert.StartsWith("Iv1.", config.ClientId);
        Assert.Equal("https://github.com/login/oauth/authorize", config.AuthorizationEndpoint);
        Assert.Equal("https://github.com/login/oauth/access_token", config.TokenEndpoint);
        Assert.Equal("https://api.github.com/user", config.UserInfoEndpoint);
    }

    [Fact]
    public void OAuthProviderConfig_MicrosoftConfiguration_IsValid()
    {
        // Arrange & Act
        var config = new OAuthProviderConfig
        {
            ProviderName = "Microsoft",
            ClientId = "abc123-def456-ghi789",
            ClientSecret = "ms-secret",
            AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            UserInfoEndpoint = "https://graph.microsoft.com/oidc/userinfo",
            CallbackPath = "/signin-microsoft"
        };

        // Assert
        Assert.Equal("Microsoft", config.ProviderName);
        Assert.Contains("-", config.ClientId);
        Assert.Equal("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", config.AuthorizationEndpoint);
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("GitHub")]
    [InlineData("Microsoft")]
    [InlineData("Facebook")]
    [InlineData("Twitter")]
    public void OAuthProviderConfig_CanSetVariousProviderNames(string providerName)
    {
        // Arrange & Act
        var config = new OAuthProviderConfig { ProviderName = providerName };

        // Assert
        Assert.Equal(providerName, config.ProviderName);
    }

    [Fact]
    public void OAuthProviderConfig_CanSetCallbackPathWithLeadingSlash()
    {
        // Arrange & Act
        var config = new OAuthProviderConfig
        {
            ProviderName = "Google",
            CallbackPath = "/signin-google"
        };

        // Assert
        Assert.StartsWith("/", config.CallbackPath);
    }

    [Fact]
    public void OAuthSettings_ClearProviders_RemovesAll()
    {
        // Arrange
        var settings = new OAuthSettings();
        settings.Providers["Google"] = new OAuthProviderConfig();
        settings.Providers["GitHub"] = new OAuthProviderConfig();

        // Act
        settings.Providers.Clear();

        // Assert
        Assert.Empty(settings.Providers);
    }

    [Fact]
    public void OAuthSettings_TryGetProvider_ReturnsCorrectly()
    {
        // Arrange
        var settings = new OAuthSettings();
        settings.Providers["Google"] = new OAuthProviderConfig { ClientId = "test-id" };

        // Act
        var found = settings.Providers.TryGetValue("Google", out var provider);

        // Assert
        Assert.True(found);
        Assert.NotNull(provider);
        Assert.Equal("test-id", provider.ClientId);
    }

    [Fact]
    public void OAuthSettings_TryGetNonExistentProvider_ReturnsFalse()
    {
        // Arrange
        var settings = new OAuthSettings();

        // Act
        var found = settings.Providers.TryGetValue("NonExistent", out _);

        // Assert
        Assert.False(found);
    }
}
