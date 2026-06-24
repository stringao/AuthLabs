using System.Security.Cryptography;
using System.Text;
using AuthLabs.ApiKey.Data;
using AuthLabs.ApiKey.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthLabs.ApiKey.Services;

/// <summary>
/// Implementacao do servico de API Keys.
/// Armazena e valida hashes de API Keys usando SHA256.
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly ApiKeyDbContext _context;

    public ApiKeyService(ApiKeyDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Valida uma API Key contra o banco de dados.
    /// Compara o hash SHA256 da key fornecida com o hash armazenado.
    /// </summary>
    public async Task<string?> ValidateApiKeyAsync(string apiKey)
    {
        var hash = ComputeHash(apiKey);
        var key = await _context.ApiKeys
            .Include(k => k.Scopes)
            .FirstOrDefaultAsync(k => k.Key == hash && k.IsActive && k.ExpiresAt > DateTime.UtcNow);

        return key?.ClientName;
    }

    /// <summary>
    /// Retorna informacoes da API Key se for valida.
    /// </summary>
    public async Task<ApiKeyInfo?> GetApiKeyInfoAsync(string apiKey)
    {
        var hash = ComputeHash(apiKey);
        var key = await _context.ApiKeys
            .Include(k => k.Scopes)
            .FirstOrDefaultAsync(k => k.Key == hash && k.IsActive && k.ExpiresAt > DateTime.UtcNow);

        if (key == null)
            return null;

        return new ApiKeyInfo
        {
            Id = key.Id,
            ClientName = key.ClientName,
            Scopes = key.Scopes.Select(s => s.Scope).ToList(),
            Role = key.Role,
            IsActive = key.IsActive
        };
    }

    /// <summary>
    /// Computa hash SHA256 de uma string.
    /// </summary>
    public static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
