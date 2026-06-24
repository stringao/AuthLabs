using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthLabs.Jwt.Tests.Integration;

public class JwtIntegrationTests : IClassFixture<JwtWebApplicationFactory>
{
    private readonly JwtWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JwtIntegrationTests(JwtWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var loginRequest = new { Email = "admin@authlabs.com", Password = "Admin123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result?.AccessToken);
        Assert.NotNull(result?.RefreshToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new { Email = "admin@authlabs.com", Password = "WrongPassword" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/protected");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        // Arrange - login first
        var loginRequest = new { Email = "admin@authlabs.com", Password = "Admin123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act - call protected endpoint with token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult?.AccessToken);
        var response = await _client.GetAsync("/api/protected");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);