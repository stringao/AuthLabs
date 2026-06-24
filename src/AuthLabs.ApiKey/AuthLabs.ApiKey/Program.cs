using AuthLabs.ApiKey.Data;
using AuthLabs.ApiKey.Models;
using AuthLabs.ApiKey.Middleware;
using AuthLabs.ApiKey.Services;
using Microsoft.EntityFrameworkCore;

// =============================================================================
// API KEYS DE DEMONSTRACAO (DADOS DE EXEMPLO)
// =============================================================================
// JUNIOR: Estes sao dados de TESTE/DESENVOLVIMENTO apenas!
// Na vida real, chaves de API sao fornecidas pelo sistema aos clientes
// de forma SEGURA (nunca hardcoded aqui).

/// <summary>
/// Dicionario de API Keys de demonstracao.
/// A chave (Key) e o valor real que o cliente usa.
/// O value e uma tupla com: (nome do cliente, scopes, role)
/// </summary>
/// <remarks>
/// JUNIOR: Por que estas chaves sao "de demonstracao"?
/// - keys "mobile-app-key-12345678" sao faceis de lembrar
/// - Scopes e roles demonstram o sistema de permissoes
/// - Uma aplicacao real teria:
    /// - Geracao automatica de keys aleatorias
    /// - Armazenamento seguro (não em codigo fonte!)
    /// - Painel do cliente para criar/revogar keys
/// </remarks>
var demoApiKeys = new Dictionary<string, (string ClientName, string[] Scopes, string Role)>
{
    // Mobile app: pode ler e escrever, role User
    { "mobile-app-key-12345678", ("mobile-app", new[] { "read", "write" }, "User") },

    // Web frontend: apenas leitura, role User
    { "web-frontend-key-87654321", ("web-frontend", new[] { "read" }, "User") },

    // Admin panel: acesso total, role Admin
    { "admin-panel-key-11223344", ("admin-panel", new[] { "read", "write", "delete" }, "Admin") },

    // External partner: apenas leitura, role Guest
    { "external-partner-key-55667788", ("external-partner", new[] { "read" }, "Guest") }
};

// =============================================================================
// CONFIGURACAO DO BUILDER
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// CONFIGURACAO DO BANCO DE DADOS POSTGRESQL
// =============================================================================

/// <summary>
/// Connection string: como conectar ao banco de dados PostgreSQL.
/// </summary>
/// <remarks>
/// JUNIOR: O que e uma connection string?
/// - String com todas as informacoes para conectar ao banco:
    /// - Host: onde o banco esta rodando
    /// - Port: porta do servico (5432 e default do PostgreSQL)
    /// - Database: nome do banco de dados
    /// - Username/Password: credenciais de acesso
/// - Em producao, NAO hardcode! Use environment variables ou secrets manager
/// </remarks>
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=authlabs_apikey;Username=postgres;Password=postgres";

/// <summary>
/// Registra o DbContext para injeccao de dependencia.
/// </summary>
/// <remarks>
/// JUNIOR: O que e AddDbContext?
/// - Registra ApiKeyDbContext no sistema de DI do ASP.NET Core
/// - Entity Framework Core cria uma instancia por requisicao (Scoped)
/// - Garante que conexoes sejam gerenciadas corretamente
/// </remarks>
builder.Services.AddDbContext<ApiKeyDbContext>(options =>
    options.UseNpgsql(connectionString));

// =============================================================================
// REGISTRO DE SERVICOS
// =============================================================================

/// <summary>
/// Registra IApiKeyService/ApiKeyService para DI.
/// </summary>
/// <remarks>
/// JUNIOR: AddScoped vs AddSingleton vs AddTransient:
/// - Scoped: uma instancia por requisicao HTTP (ideal para DbContext)
/// - Singleton: uma instancia para toda a aplicacao (estado global)
/// - Transient: nova instancia a cada vez que e solicitado
/// - IApiKeyService e Scoped pois usa DbContext (que tambem e Scoped)
/// </remarks>
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// =============================================================================
// CONFIGURACAO DE SERVICES ADICIONAIS
// =============================================================================

// Adiciona suporte a Controllers (MVC)
builder.Services.AddControllers();

/// <summary>
/// Health checks: verificam se a aplicacao esta saudavel.
/// </summary>
/// <remarks>
/// JUNIOR: O que sao health checks?
/// - Endpoint que retorna "200 OK" se app esta funcionando
/// - Usado por:
    /// - Load balancers para saber se mandam trafego
    /// - Kubernetes para readiness/liveness probes
    /// - Monitoramento (Datadog, New Relic, etc)
/// - /health ja esta configurado mais abaixo com MapHealthChecks()
/// </remarks>
builder.Services.AddHealthChecks();

// =============================================================================
// CONSTRUCAO DA APLICACAO
// =============================================================================

var app = builder.Build();

// =============================================================================
// SEED DE DADOS (POPULAR BANCO INICIAL)
// =============================================================================

/// <summary>
/// Bloco using: garante que recursos sejam liberados ao final.
/// </summary>
/// <remarks>
/// JUNIOR: O que e CreateScope()?
/// - Cria um escopo de injecao de dependencia
/// - Necessario para acessar services dentro de tasks assincronas
/// - O scope e descartado ao fim do bloco using
/// </remarks>
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiKeyDbContext>();

    // Garante que o banco e as tabelas existam
    await context.Database.EnsureCreatedAsync();

    // So popula se banco estiver vazio (evita duplicacao)
    if (!await context.ApiKeys.AnyAsync())
    {
        // Para cada key de demonstracao, cria registro no banco
        foreach (var (key, (clientName, scopes, role)) in demoApiKeys)
        {
            // Computa hash da key (nao armazena texto plano!)
            var hash = ApiKeyService.ComputeHash(key);

            // Cria entidade ApiKey com todos os dados
            var apiKey = new ApiKey
            {
                Key = hash,              // Hash SHA256
                ClientName = clientName,
                Role = role,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddYears(1), // JUNIOR: Expira em 1 ano
                CreatedAt = DateTime.UtcNow,
                Scopes = scopes.Select(s => new ApiKeyScope { Scope = s }).ToList()
            };

            context.ApiKeys.Add(apiKey);
        }

        // Salva todas as changes no banco de uma vez
        await context.SaveChangesAsync();

        // Imprime as chaves no console (so visivel no servidor!)
        Console.WriteLine("=== API Keys de Demonstracao ===");
        Console.WriteLine("IMPORTANTE: Guarde estas chaves - elas NAO podem ser recuperadas!");
        Console.WriteLine("Use-as no header X-Api-Key para fazer requisicoes autenticadas.");
        foreach (var (key, (clientName, _, _)) in demoApiKeys)
        {
            Console.WriteLine($"  {clientName}: {key}");
        }
        Console.WriteLine("================================");
    }
}

// =============================================================================
// CONFIGURACAO DO PIPELINE DE MIDDLEWARE
// =============================================================================

// Endpoint de health check (ANTES do middleware de auth - nao precisa auth)
app.MapHealthChecks("/health");

// MIDDLEWARE DE AUTENTICACAO API KEY
// JUNIOR: Ordem importa! Middlewares executam em sequencia.
app.UseApiKeyAuthentication();

/// <summary>
/// UseAuthorization: verifica se usuario tem permissao (Baseado em [Authorize]).
/// </summary>
/// <remarks>
/// JUNIOR: Autenticacao vs Autorizacao:
/// - Autenticacao (UseApiKeyAuthentication): "Quem voce e?"
/// - Autorizacao (UseAuthorization): "Voce tem permissao?"
/// - UseAuthorization geralmente vem DEPOIS de autenticacao
/// - Mas como temos middleware custom, authorization vem depois
/// </remarks>
app.UseAuthorization();

// Mapeia controllers (rotas definidas com [Route] nos controllers)
app.MapControllers();

// =============================================================================
// INICIALIZACAO
// =============================================================================

// Run inicia o servidor web
app.Run();

// JUNIOR: Esta classe partial existe para permitir testes de integracao.
// Testes podem acessar Program sem rodar o servidor completo.
public partial class Program { }
