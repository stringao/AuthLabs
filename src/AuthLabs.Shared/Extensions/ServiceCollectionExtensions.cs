using AuthLabs.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthLabs.Shared.Extensions;

/// <summary>
/// Extensões reutilizáveis para configuração de serviços.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona o DbContext compartilhado com PostgreSQL.
    /// </summary>
    public static IServiceCollection AddSharedDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Adiciona o DbContext compartilhado com SQLite em memória (para testes).
    /// </summary>
    public static IServiceCollection AddSharedDbContextInMemory(
        this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        return services;
    }
}