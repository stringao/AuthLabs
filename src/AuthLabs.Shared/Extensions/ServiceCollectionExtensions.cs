using AuthLabs.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthLabs.Shared.Extensions;

/// <summary>
/// Métodos de extensão para configurar serviços de infraestrutura.
///
/// JUNIOR: O que são Extension Methods?
/// ============================================================================
/// Extension methods permitem que você adicione métodos a classes EXISTENTES
/// sem modificar o código original da classe.
///
/// Exemplo:
///   // Em vez de fazer isso:
///   var result = ServiceCollectionExtensions.AddSharedDbContext(services, "string");
///
///   // Você pode fazer isso (sintaxe mais limpa):
///   services.AddSharedDbContext("string");
///
/// A diferença é que o segundo parece um método "nativo" do IServiceCollection.
///
/// Como funciona:
///   - O primeiro parâmetro (this IServiceCollection services) é a classe
///     que está sendo extendida
///   - Você pode chamar como se fosse um método de instância
///   - É só um "açúcar sintático" - o compilador converte para o primeiro formato
///
/// Por que usar?
///   - Código mais legível: services.AddSharedDbContext() é mais claro que
///     ServiceCollectionHelper.AddSharedDbContext(services)
///   - Encapsula configuração complexa em métodos simples
///   - Permite criar uma "API fluente" (method chaining)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra o DbContext compartilhado configurado para PostgreSQL.
    ///
    /// JUNIOR: O que é " IServiceCollection"?
    /// ============================================================================
    /// IServiceCollection é a coleção de serviços do ASP.NET Core.
    /// Quando você registra um serviço aqui, o ASP.NET pode gerenciar seu ciclo
    /// de vida e injetá-lo onde for necessário.
    ///
    /// Padrões de ciclo de vida:
    ///   - AddSingleton: uma instância para TODO o aplicativo (comum para caches)
    ///   - AddScoped: uma instância por requisição HTTP (comum para DbContext)
    ///   - AddTransient: uma nova instância cada vez que é requerida (leves)
    ///
    /// Por que DbContext é Scoped?
    ///   - Cada requisição HTTP deve ter seu próprio DbContext
    ///   - Evita compartilhar estado entre requisições concorrentes
    ///   - Após a requisição terminar, o DbContext é descartado (importante!)
    ///
    /// JUNIOR: O que é " connectionString"?
    /// ============================================================================
    /// Connection string é a "morada" do seu banco de dados.
    ///
    /// Exemplo para PostgreSQL:
    ///   "Host=localhost;Database=meubanco;Username=usuario;Password=senha"
    ///
    /// Partes da connection string:
    ///   - Host: onde o banco está rodando (localhost = sua própria máquina)
    ///   - Database: nome do banco de dados (criado automaticamente se não existir)
    ///   - Username/Password: credenciais de acesso ao banco
    ///   - Port: opcional (5432 é a default do PostgreSQL)
    ///
    /// Onde guardar connection strings?
    ///   - NUNCA hardcode no código! (vazaria no git, expondo segredos)
    ///   - Use: environment variables, secrets manager, appsettings.json
    ///   - Em produção: secrets externos como Azure Key Vault, AWS Secrets Manager
    /// </summary>
    /// <param name="services">A coleção de serviços do aplicativo</param>
    /// <param name="connectionString">String de conexão do PostgreSQL</param>
    /// <returns>A mesma coleção de serviços (para method chaining)</returns>
    public static IServiceCollection AddSharedDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        // =========================================================================
        // JUNIOR: AddDbContext vs AddDbContextFactory
        // =========================================================================
        // AddDbContext: o ASP.NET cria e gerencia o DbContext automaticamente
        //   - Vantagem: simples, integrado com DI
        //   - Desvantagem: não tão fácil de controlar em cenários avançados
        //
        // AddDbContextFactory: você cria instâncias quando precisar
        //   - Vantagem: mais controle, melhor para testes
        //   - Desvantagem: mais código para gerenciar
        //
        // Para a maioria das aplicações, AddDbContext é suficiente.
        // =========================================================================

        // =========================================================================
        // JUNIOR: UseNpgsql
        // =========================================================================
        // UseNpgsql() configura o EF Core para usar PostgreSQL como banco.
        //
        // O que acontece internamente:
        //   1. Registra Npgsql como o "provider" do banco
        //   2. Configura como gerar comandos SQL específicos do PostgreSQL
        //   3. Permite usar recursos exclusivos do PostgreSQL (JSON, arrays, etc.)
        //
        // Outros providers populares:
        //   - UseSqlServer: Microsoft SQL Server
        //   - UseMySql: MySQL / MariaDB
        //   - UseSqlite: SQLite (banco em arquivo, bom para mobiles/desktop)
        //   - UseInMemoryDatabase: banco em memória (para testes)
        //
        // JUNIOR TIP: Se você mudar de banco (ex: PostgreSQL → SQL Server),
        // basta mudar UseNpgsql para UseSqlServer e a connection string.
        // Seu código C# de consultas pode permanecer o mesmo (na maioria dos casos)!
        // =========================================================================
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Retorna services para permitir method chaining:
        // services.AddSharedDbContext("conn").AddSomethingElse();
        return services;
    }

    /// <summary>
    /// Registra o DbContext compartilhado usando banco de dados em memória.
    ///
    /// JUNIOR: Para que serve este método?
    /// ============================================================================
    /// Este método é USADO EXCLUSIVAMENTE PARA TESTES AUTOMATIZADOS.
    ///
    /// Vantagens do banco em memória para testes:
    ///   - Não precisa instalar PostgreSQL ou outro banco
    ///   - Testes rodam mais rápido (memória RAM >> disco)
    ///   - Cada teste pode ter seu próprio banco limpo
    ///   - Testes são isolados (não afetam outros testes)
    ///   - Ótimo para CI/CD (integração contínua)
    ///
    /// Desvantagens:
    ///   - Não é um banco real (não testa queries SQL específicas)
    ///   - Não testa performance real
    ///   - Alguns recursos do PostgreSQL não funcionam em memória
    ///
    /// Quando usar?
    ///   - Testes de unidade (unit tests)
    ///   - Testes de integração básicos
    ///   - Quando precisa de um banco rapido e descartável
    ///
    /// Como usar em testes:
    ///   [Fact]
    ///   public void Test_SomeThing()
    ///   {
    ///       // Arrange: configura o serviço com banco em memória
    ///       var services = new ServiceCollection();
    ///       services.AddSharedDbContextInMemory();
    ///       var provider = services.BuildServiceProvider();
    ///
    ///       // Act: usa o contexto
    ///       using var context = provider.GetRequiredService<AppDbContext>();
    ///       context.Users.Add(new User { UserName = "test" });
    ///       await context.SaveChangesAsync();
    ///
    ///       // Assert: verifica resultado
    ///       Assert.Equal(1, context.Users.Count());
    ///   }
    /// </summary>
    /// <param name="services">A coleção de serviços do aplicativo</param>
    /// <returns>A mesma coleção de serviços (para method chaining)</returns>
    public static IServiceCollection AddSharedDbContextInMemory(
        this IServiceCollection services)
    {
        // =========================================================================
        // JUNIOR: "TestDb" como nome do banco
        // =========================================================================
        // O nome "TestDb" é arbitrário - pode ser qualquer string única.
        //
        // Pontos importantes:
        //   - Cada chamada a UseInMemoryDatabase com o MESMO nome compartilha
        //     o mesmo banco em memória (cuidado com isolamento de testes!)
        //   - Para garantir isolamento, use nomes únicos por teste:
        //     UseInMemoryDatabase(Guid.NewGuid().ToString())
        //
        //   Mas para a maioria dos casos de uso, um nome fixo como "TestDb"
        //   é suficiente e desejável (simplifica o código).
        // =========================================================================

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        return services;
    }

    // =========================================================================
    // JUNIOR: Por que retornar IServiceCollection?
    // =========================================================================
    // Retornar IServiceCollection permite "method chaining":
    //
    //   services
    //       .AddSharedDbContext(connectionString)
    //       .AddAuthentication()
    //       .AddAuthorization();
    //
    // Isso é chamado de "Fluent API" - código que lê como uma sentença.
    //
    // É apenas conveniência - você NÃO é obrigado a usar o retorno.
    // Você pode ignorar o retorno se preferir:
    //
    //   services.AddSharedDbContext(connectionString);
    //   services.AddAuthentication();
    //
    // =========================================================================
}
