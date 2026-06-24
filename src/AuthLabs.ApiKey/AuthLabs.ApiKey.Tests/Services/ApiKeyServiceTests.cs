using AuthLabs.ApiKey.Data;
using AuthLabs.ApiKey.Services;
using Microsoft.EntityFrameworkCore;
using ApiKeyModel = AuthLabs.ApiKey.Models.ApiKey;
using ApiKeyScopeModel = AuthLabs.ApiKey.Models.ApiKeyScope;

namespace AuthLabs.ApiKey.Tests.Services;

public class ApiKeyServiceTests : IDisposable
{
    private readonly ApiKeyDbContext _context;
    private readonly ApiKeyService _service;

    public ApiKeyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApiKeyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApiKeyDbContext(options);
        _service = new ApiKeyService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ComApiKeyValida_RetornaClientName()
    {
        // Arrange
        var plainKey = "test-api-key-12345";
        var hash = ApiKeyService.ComputeHash(plainKey);
        var apiKey = new ApiKeyModel
        {
            Key = hash,
            ClientName = "test-client",
            Role = "User",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Scopes = new List<ApiKeyScopeModel>
            {
                new() { Scope = "read" },
                new() { Scope = "write" }
            }
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateApiKeyAsync(plainKey);

        // Assert
        Assert.Equal("test-client", result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ComApiKeyInvalida_RetornaNull()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync("invalid-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ComApiKeyExpirada_RetornaNull()
    {
        // Arrange
        var plainKey = "expired-key-12345";
        var hash = ApiKeyService.ComputeHash(plainKey);
        var apiKey = new ApiKeyModel
        {
            Key = hash,
            ClientName = "expired-client",
            Role = "User",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expirada
            Scopes = new List<ApiKeyScopeModel>()
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateApiKeyAsync(plainKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ComApiKeyInativa_RetornaNull()
    {
        // Arrange
        var plainKey = "inactive-key-12345";
        var hash = ApiKeyService.ComputeHash(plainKey);
        var apiKey = new ApiKeyModel
        {
            Key = hash,
            ClientName = "inactive-client",
            Role = "User",
            IsActive = false, // Inativa
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Scopes = new List<ApiKeyScopeModel>()
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateApiKeyAsync(plainKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetApiKeyInfoAsync_ComApiKeyValida_RetornaApiKeyInfo()
    {
        // Arrange
        var plainKey = "info-test-key-12345";
        var hash = ApiKeyService.ComputeHash(plainKey);
        var apiKey = new ApiKeyModel
        {
            Key = hash,
            ClientName = "info-client",
            Role = "Admin",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Scopes = new List<ApiKeyScopeModel>
            {
                new() { Scope = "read" },
                new() { Scope = "write" },
                new() { Scope = "delete" }
            }
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetApiKeyInfoAsync(plainKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("info-client", result.ClientName);
        Assert.Equal("Admin", result.Role);
        Assert.Equal(3, result.Scopes.Count);
        Assert.Contains("read", result.Scopes);
        Assert.Contains("write", result.Scopes);
        Assert.Contains("delete", result.Scopes);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetApiKeyInfoAsync_ComApiKeyInvalida_RetornaNull()
    {
        // Act
        var result = await _service.GetApiKeyInfoAsync("invalid-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ComputeHash_ProduceHashConsistente()
    {
        // Arrange
        var input = "consistent-key-12345";

        // Act
        var hash1 = ApiKeyService.ComputeHash(input);
        var hash2 = ApiKeyService.ComputeHash(input);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(input, hash1); // Hash deve ser diferente da entrada
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ComHashDiferentes_RetornaNull()
    {
        // Arrange - mesmo hash diferente de key diferente
        var hash = ApiKeyService.ComputeHash("key-1");
        var apiKey = new ApiKeyModel
        {
            Key = hash,
            ClientName = "hash-test",
            Role = "User",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Scopes = new List<ApiKeyScopeModel>()
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Act - usa key diferente
        var result = await _service.ValidateApiKeyAsync("key-2");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithNullKey_ReturnsAppropriateResponse()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithEmptyKey_ReturnsAppropriateResponse()
    {
        // Act
        var resultNull = await _service.ValidateApiKeyAsync(null!);
        var resultEmpty = await _service.ValidateApiKeyAsync("");
        var resultWhitespace = await _service.ValidateApiKeyAsync("   ");

        // Assert
        Assert.Null(resultNull);
        Assert.Null(resultEmpty);
        Assert.Null(resultWhitespace);
    }

    [Fact]
    public async Task GetApiKeyInfoAsync_WithNullKey_ReturnsAppropriateResponse()
    {
        // Act
        var resultNull = await _service.GetApiKeyInfoAsync(null!);
        var resultEmpty = await _service.GetApiKeyInfoAsync("");
        var resultWhitespace = await _service.GetApiKeyInfoAsync("   ");

        // Assert
        Assert.Null(resultNull);
        Assert.Null(resultEmpty);
        Assert.Null(resultWhitespace);
    }

    [Fact]
    public void ComputeHash_IsDeterministic()
    {
        // Arrange
        var input = "deterministic-key-test-12345";

        // Act
        var hash1 = ApiKeyService.ComputeHash(input);
        var hash2 = ApiKeyService.ComputeHash(input);
        var hash3 = ApiKeyService.ComputeHash(input);

        // Assert - same input must produce same output every time
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
        Assert.Equal(hash1, hash3);
    }
}
