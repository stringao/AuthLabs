using AuthLabs.ApiKey.Data;
using AuthLabs.ApiKey.Models;
using AuthLabs.ApiKey.Middleware;
using AuthLabs.ApiKey.Services;
using Microsoft.EntityFrameworkCore;

// API Keys de demonstracao (plain text - mostrar ao usuario, nunca armazenar)
var demoApiKeys = new Dictionary<string, (string ClientName, string[] Scopes, string Role)>
{
    { "mobile-app-key-12345678", ("mobile-app", new[] { "read", "write" }, "User") },
    { "web-frontend-key-87654321", ("web-frontend", new[] { "read" }, "User") },
    { "admin-panel-key-11223344", ("admin-panel", new[] { "read", "write", "delete" }, "Admin") },
    { "external-partner-key-55667788", ("external-partner", new[] { "read" }, "Guest") }
};

var builder = WebApplication.CreateBuilder(args);

// Configura PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=authlabs_apikey;Username=postgres;Password=postgres";
builder.Services.AddDbContext<ApiKeyDbContext>(options =>
    options.UseNpgsql(connectionString));

// Registra servicos
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// Adiciona controllers
builder.Services.AddControllers();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiKeyDbContext>();
    await context.Database.EnsureCreatedAsync();

    // Verifica se ja existem chaves
    if (!await context.ApiKeys.AnyAsync())
    {
        foreach (var (key, (clientName, scopes, role)) in demoApiKeys)
        {
            var hash = ApiKeyService.ComputeHash(key);
            var apiKey = new ApiKey
            {
                Key = hash,
                ClientName = clientName,
                Role = role,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                Scopes = scopes.Select(s => new ApiKeyScope { Scope = s }).ToList()
            };
            context.ApiKeys.Add(apiKey);
        }
        await context.SaveChangesAsync();

        Console.WriteLine("=== API Keys de Demonstracao ===");
        Console.WriteLine("Guarde estas chaves - elas nao podem ser recuperadas:");
        foreach (var (key, (clientName, _, _)) in demoApiKeys)
        {
            Console.WriteLine($"  {clientName}: {key}");
        }
        Console.WriteLine("================================");
    }
}

// Health check endpoint (skip auth)
app.MapHealthChecks("/health");

// Middleware de autenticacao API Key
app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Torna Program publica para testes de integracao
public partial class Program { }
