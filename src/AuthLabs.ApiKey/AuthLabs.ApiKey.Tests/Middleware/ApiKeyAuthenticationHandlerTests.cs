using System.Security.Claims;
using System.Text;
using AuthLabs.ApiKey.Middleware;
using AuthLabs.ApiKey.Models;
using AuthLabs.ApiKey.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace AuthLabs.ApiKey.Tests.Middleware;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IApiKeyService> _mockApiKeyService;

    public ApiKeyAuthenticationHandlerTests()
    {
        _mockApiKeyService = new Mock<IApiKeyService>();
    }

    private DefaultHttpContext CreateHttpContext(string path = "/test", string? apiKeyHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (apiKeyHeader != null)
        {
            context.Request.Headers["X-Api-Key"] = apiKeyHeader;
        }

        return context;
    }

    private async Task<string> ReadResponseBody(Stream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_SwaggerPath_SkipsAuthentication()
    {
        // Arrange
        var context = CreateHttpContext("/swagger/index.html");
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.True(nextCalled);
        _mockApiKeyService.Verify(s => s.GetApiKeyInfoAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_HealthPath_SkipsAuthentication()
    {
        // Arrange
        var context = CreateHttpContext("/health");
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.True(nextCalled);
        _mockApiKeyService.Verify(s => s.GetApiKeyInfoAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_MissingApiKeyHeader_Returns401()
    {
        // Arrange
        var context = CreateHttpContext("/api/test");
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        var responseBody = await ReadResponseBody(context.Response.Body);
        Assert.Contains("API Key nao fornecida", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_EmptyApiKey_Returns401()
    {
        // Arrange
        var context = CreateHttpContext("/api/test", "");
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        var responseBody = await ReadResponseBody(context.Response.Body);
        Assert.Contains("API Key nao pode ser vazia", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_WhitespaceApiKey_Returns401()
    {
        // Arrange
        var context = CreateHttpContext("/api/test", "   ");
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        var responseBody = await ReadResponseBody(context.Response.Body);
        Assert.Contains("API Key nao pode ser vazia", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_InvalidApiKey_Returns401()
    {
        // Arrange
        var context = CreateHttpContext("/api/test", "invalid-key");
        _mockApiKeyService.Setup(s => s.GetApiKeyInfoAsync("invalid-key"))
            .ReturnsAsync((ApiKeyInfo?)null);
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        var responseBody = await ReadResponseBody(context.Response.Body);
        Assert.Contains("API Key invalida ou expirada", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_ValidApiKey_SetsUserAndCallsNext()
    {
        // Arrange
        var apiKeyInfo = new ApiKeyInfo
        {
            Id = 1,
            ClientName = "test-client",
            Role = "Admin",
            Scopes = new List<string> { "read", "write" },
            IsActive = true
        };
        var context = CreateHttpContext("/api/test", "valid-key");
        _mockApiKeyService.Setup(s => s.GetApiKeyInfoAsync("valid-key"))
            .ReturnsAsync(apiKeyInfo);

        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.NotNull(context.User);
        Assert.Equal("test-client", context.User.Identity?.Name);
        Assert.True(context.User.IsInRole("Admin"));
    }

    [Fact]
    public async Task InvokeAsync_ValidApiKey_CreatesCorrectClaims()
    {
        // Arrange
        var apiKeyInfo = new ApiKeyInfo
        {
            Id = 42,
            ClientName = "claims-test-client",
            Role = "User",
            Scopes = new List<string> { "read", "write", "delete" },
            IsActive = true
        };
        var context = CreateHttpContext("/api/test", "claims-key");
        _mockApiKeyService.Setup(s => s.GetApiKeyInfoAsync("claims-key"))
            .ReturnsAsync(apiKeyInfo);

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var handler = new ApiKeyAuthenticationHandler(next);

        // Act
        await handler.InvokeAsync(context, _mockApiKeyService.Object);

        // Assert
        var claims = context.User.Claims.ToList();

        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == "claims-test-client");
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
        Assert.Contains(claims, c => c.Type == "apikey_id" && c.Value == "42");
        Assert.Contains(claims, c => c.Type == "client_name" && c.Value == "claims-test-client");
        Assert.Contains(claims, c => c.Type == "scope" && c.Value == "read");
        Assert.Contains(claims, c => c.Type == "scope" && c.Value == "write");
        Assert.Contains(claims, c => c.Type == "scope" && c.Value == "delete");
    }
}
