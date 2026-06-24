using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthLabs.Shared.Data;

/// <summary>
/// DbContext principal da aplicação.
///
/// JUNIOR: O que é um DbContext?
/// ============================================================================
/// DbContext é a classe central do Entity Framework Core. Pense nele como:
///   - O "tradutor" entre seu código C# e o banco de dados SQL
///   - O objeto que gerencia TODAS as operações com o banco
///   - Uma forma de representar o banco de dados como objetos C#
///
/// O que ele faz?
///   - Mapeia suas classes (como User, RefreshToken) para tabelas
///   - Permite fazer consultas ao banco usando LINQ (bem mais fácil que SQL)
///   - Mantém controle de alterações para salvar no banco
///   - Gerencia transações automaticamente
///
/// Como usar:
///   using var context = new AppDbContext(options);
///   var users = context.Users.ToList(); // Busca todos os usuários
/// </summary>
public class AppDbContext : IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole<int>, int>
{
    /// <summary>
    /// Constructor que recebe as configurações do banco de dados.
    ///
    /// JUNIOR: Por que precisamos de um constructor especial?
    ///   - O DbContext PRECISA saber COMO se conectar ao banco
    ///   - As opções (connection string, provider, etc.) vem via injeção
    ///   - Este constructor é chamado pelo ASP.NET quando registra o serviço
    ///
    /// O parâmetro "options" contém:
    ///   - Qual banco usar (PostgreSQL, SQL Server, SQLite, etc.)
    ///   - A string de conexão (onde o banco está, nome, credenciais)
    ///   - Configurações adicionais (timeout, pool size, etc.)
    /// </summary>
    /// <param name="options">Configurações do Entity Framework Core</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Coleção de RefreshTokens no banco de dados.
    ///
    /// JUNIOR: DbSet é como você acessa uma tabela pelo EF Core.
    ///   - DbSet<RefreshToken> representa a tabela RefreshTokens
    ///   - O nome "RefreshTokens" é pluralizado automaticamente pelo EF
    ///   - Você pode fazer: context.RefreshTokens.Where(...) // busca
    ///   - Ou: context.RefreshTokens.Add(newToken) // inserção
    ///
    /// Esta propriedade é do tipo "DbSet<RefreshToken>" que fornece métodos como:
    ///   - Find(id): busca por ID primário
    ///   - Where(condição): filtra registros
    ///   - Add(entity): adiciona novo registro
    ///   - Remove(entity): remove registro
    ///   - ToList(): busca todos os registros
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Configura como as entidades são mapeadas para o banco de dados.
    ///
    /// JUNIOR: OnModelCreating é chamado QUANDO O BANCO É CRIADO.
    ///   - Aqui você define detalhes que o EF não infere automaticamente
    ///   - É onde você configura: índices, relações, restrições, etc.
    ///   - Sempre chame "base.OnModelCreating(builder)" primeiro!
    ///     (Isso garante que a configuração do Identity seja aplicada)
    ///
    /// Dica: Você também pode usar Data Annotations (atributos na classe)
    /// em vez de Fluent API. Mas Fluent API é mais flexível e limpo.
    /// </summary>
    /// <param name="builder">Construtor de modelo do EF Core</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // JUNIOR: Primeiro, aplica TODA a configuração do Identity!
        // O Identity tem muitas tabelas (Users, Roles, RoleClaims, UserLogins,
        // UserRoles, UserTokens, etc.) e elas precisam ser configuradas.
        // Sem esta linha, o sistema de login não funcionará corretamente!
        base.OnModelCreating(builder);

        // JUNIOR: Configuração específica do RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            // =========================================================================
            // JUNIOR: Por que criar um índice único no Token?
            // =========================================================================
            // Um índice é como o "índice de um livro" - acelera buscas.
            //
            // Por que o Token precisa ser único?
            //   1. Segurança: cada refresh token deve ser usado apenas uma vez
            //   2. Integridade: não pode haver dois tokens com o mesmo valor
            //   3. Performance: buscas por token (validação) ficam mais rápidas
            //
            // Como funciona na prática?
            //   - Quando você faz context.RefreshTokens.Where(rt => rt.Token == "abc")
            //   - O banco usa o índice em vez de escanear toda a tabela
            //   - Em tabelas pequenas não faz diferença, mas com milhões de
            //     registros, pode ser a diferença de ms para segundos!
            //
            // JUNIOR TIP: O EF Core infere que "Token" é unique por causa do
            // ".IsUnique()", então ele cria um índice UNIQUE.
            entity.HasIndex(rt => rt.Token).IsUnique();

            // =========================================================================
            // JUNIOR: Configurando a relação entre RefreshToken e User
            // =========================================================================
            // RefreshToken tem um UserId (chave estrangeira)
            // User tem uma coleção de RefreshTokens (relação 1:N)
            //
            // O que este código faz?
            //   HasOne(rt => rt.User)           → "RefreshToken TEM UM User"
            //   .WithMany()                     → "User PODE TER MUITOS RefreshTokens"
            //   .HasForeignKey(rt => rt.UserId) → "A ligação é pelo campo UserId"
            //   .OnDelete(DeleteBehavior.Cascade) → "Se User for deletado, tokens também são"
            //
            // JUNIOR: DeleteBehavior.Cascade é IMPORTANTE!
            //   - Se você deletar um usuário, TODOS os seus refresh tokens
            //     serão automaticamente deletados também
            //   - Isso evita "órfãos" no banco (tokens sem usuário)
            //
            //   Outros DeleteBehavior:
            //   - Cascade: deleta os filhos junto (usamos este)
            //   - SetNull: seta o UserId como null (não funciona aqui, é NOT NULL)
            //   - Restrict: impede deletar se houver filhos
            //   - NoAction: não faz nada no banco (pode deixar órfãos!)
            //
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
