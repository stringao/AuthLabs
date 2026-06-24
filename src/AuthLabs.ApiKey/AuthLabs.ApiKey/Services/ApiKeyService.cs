using System.Security.Cryptography;
using System.Text;
using AuthLabs.ApiKey.Data;
using AuthLabs.ApiKey.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthLabs.ApiKey.Services;

/// <summary>
/// Implementacao do servico de API Keys.
/// Responsavel por validar e gerenciar API Keys usando hash SHA256.
/// </summary>
/// <remarks>
/// JUNIOR: O que e um Service?
/// - Services contem a LOGICA DE NEGOCIO da aplicacao
/// - Diferente de Controllers (que lidam com HTTP)
/// - Services sao reutilizaveis em toda a aplicacao
/// - Este service e "inyectado" (Injecao de Dependencia) onde for preciso
/// </remarks>
public class ApiKeyService : IApiKeyService
{
    private readonly ApiKeyDbContext _context;

    /// <summary>
    /// Construtor que recebe o DbContext via injecao de dependencia.
    /// </summary>
    /// <param name="context">Contexto do banco de dados Entity Framework</param>
    /// <remarks>
    /// JUNIOR: Por que receber o context assim?
    /// - Isso e chamada "Injecao de Dependencia" (DI)
    /// - O ASP.NET Core cria e gerencia o lifecycle do DbContext
    /// - Garantia que temos uma instancia valida quando precisamos
    /// - Facilita testes (podemos mockar o DbContext)
    /// </remarks>
    public ApiKeyService(ApiKeyDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Valida uma API Key e retorna o nome do cliente se for valida.
    /// </summary>
    /// <param name="apiKey">A API Key em texto plano fornecida pelo cliente</param>
    /// <returns>Nome do cliente se valida, ou null se invalida/expirada</returns>
    /// <remarks>
    /// JUNIOR: Fluxo de validacao de API Key:
    /// 1. Receber a key em texto plano do cliente
    /// 2. Computar o hash SHA256 dessa key
    /// 3. Buscar no banco uma key com hash correspondente
    /// 4. Verificar se IsActive=true E ExpiresAt > agora
    /// 5. Se tudo ok, a key e valida
    ///
    /// JUNIOR: Por que comparamos HASH e nao a key diretamente?
    /// - Se o banco for comprometido, atacante NAO tera as keys reais
    /// - Hashing e one-way: nao da pra reverter
    /// - Cliente envia plain text -> nos hashamos -> comparamos com banco
    /// </remarks>
    public async Task<string?> ValidateApiKeyAsync(string apiKey)
    {
        // JUNIOR: Validacao de seguranca - nunca processar null ou vazio
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var hash = ComputeHash(apiKey);

        // JUNIOR: FirstOrDefaultAsync retorna null se nao encontrar
        // LINQ com Entity Framework: o .Include traz os Scopes relacionados
        var key = await _context.ApiKeys
            .Include(k => k.Scopes) // JUNIOR: Carrega escopos relacionados em uma query
            .FirstOrDefaultAsync(k =>
                k.Key == hash &&    // Hash deve corresponder
                k.IsActive &&       // Deve estar ativa
                k.ExpiresAt > DateTime.UtcNow); // Nao deve ter expirado

        // JUNIOR: Operador ?. retorna null se key for null (null-conditional)
        return key?.ClientName;
    }

    /// <summary>
    /// Retorna informacoes detalhadas da API Key se for valida.
    /// </summary>
    /// <param name="apiKey">A API Key em texto plano</param>
    /// <returns>Objeto ApiKeyInfo com dados do cliente, ou null se invalida</returns>
    /// <remarks>
    /// JUNIOR: Diferenca entre ValidateApiKeyAsync e GetApiKeyInfoAsync:
    /// - Validate: retorna apenas o nome (verificacao rapida)
    /// - GetApiKeyInfo: retorna DTO completo (para usar no contexto HTTP)
    /// - GetApiKeyInfo e usado no middleware para criar a identidade do usuario
    /// </remarks>
    public async Task<ApiKeyInfo?> GetApiKeyInfoAsync(string apiKey)
    {
        // JUNIOR: Protecao contra null - se apiKey for null, ComputeHash lancaria excessao
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var hash = ComputeHash(apiKey);
        var key = await _context.ApiKeys
            .Include(k => k.Scopes)
            .FirstOrDefaultAsync(k =>
                k.Key == hash &&
                k.IsActive &&
                k.ExpiresAt > DateTime.UtcNow);

        // JUNIOR: Se nao encontrou, retorna null (autenticacao falhou)
        if (key == null)
            return null;

        // JUNIOR: Mapeia entidade -> DTO (separacao de camadas)
        return new ApiKeyInfo
        {
            Id = key.Id,
            ClientName = key.ClientName,
            // JUNIOR: Select projeta apenas os nomes dos scopes
            Scopes = key.Scopes.Select(s => s.Scope).ToList(),
            Role = key.Role,
            IsActive = key.IsActive
        };
    }

    /// <summary>
    /// Computa hash SHA256 de uma string de entrada.
    /// </summary>
    /// <param name="input">String a ser hasheada</param>
    /// <returns>Hash em formato Base64</returns>
    /// <remarks>
    /// JUNIOR: Como funciona o hashing aqui:
    /// 1. Convert.ToBase64String(bytes) -> converte bytes para string Base64
    /// 2. SHA256.HashData() -> algoritmo de hash criptografico
    /// 3. Same input = Same output (deterministico)
    ///
    /// JUNIOR: Por que Base64?
    /// - Bytes brutos podem conter caracteres invalidos em strings
    /// - Base64 e "string-safe" e compactavel
    /// - Armazenamos como VARCHAR no banco, nao BLOB
    /// </remarks>
    public static string ComputeHash(string input)
    {
        // JUNIOR: Encoding.UTF8.GetBytes() converte string para array de bytes
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
