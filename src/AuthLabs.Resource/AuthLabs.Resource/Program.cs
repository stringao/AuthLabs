using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AuthLabs.Resource.Authorization;
using AuthLabs.Resource.Data;
using AuthLabs.Resource.Services;
using AuthLabs.Resource.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthLabs.Resource API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JUNIOR: Configuração de Autenticação JWT
// JWT (JSON Web Token) é o formato do token de autenticação usado
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // JUNIOR: TokenValidationParameters define como validar o token JWT
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           // Valida quem emitiu o token
        ValidateAudience = true,         // Valida para quem o token foi emitido
        ValidateLifetime = true,         // Verifica se o token não expirou
        ValidateIssuerSigningKey = true, // Verifica a assinatura do token
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// JUNIOR: Banco de dados InMemory para demonstração
// Em produção, usaria SQL Server, PostgreSQL, etc.
builder.Services.AddDbContext<ResourceDbContext>(options =>
    options.UseInMemoryDatabase("AuthLabsResourceDb"));

// JUNIOR: CONFIGURAÇÃO DE AUTORIZAÇÃO BASEADA EM RECURSOS
// Aqui registramos o handler que vai evaluar permissões em documentos específicos
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();

// JUNIOR: Registro do serviço de documentos
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

// JUNIOR: Seed de dados - cria dados iniciais para teste
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ResourceDbContext>();
    await SeedDataAsync(context);
}

/// <summary>
///.seedDataAsync - Preenche o banco com dados iniciais para demonstração.
/// </summary>
/// <remarks>
/// <para>
/// <b>JUNIOR: O que é seed de dados?</b>
/// É o processo de popular o banco com dados iniciais quando a aplicação inicia.
/// Útil para ambientes de desenvolvimento e demonstração.
/// </para>
/// <para>
/// <b>Usuários criados:</b>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>ID</term>
/// <description>Email</description>
/// </listheader>
/// <item>
/// <term>1</term>
/// <description>admin@authlabs.com (Admin)</description>
/// </item>
/// <item>
/// <term>2</term>
/// <description>manager@authlabs.com (Gerente)</description>
/// </item>
/// <item>
/// <term>3</term>
/// <description>user@authlabs.com (Usuário comum)</description>
/// </item>
/// <item>
/// <term>4</term>
/// <description>guest@authlabs.com (Convidado)</description>
/// </item>
/// </list>
/// <para>
/// <b>Documentos e permissões:</b>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Doc ID</term>
/// <term>Proprietário</term>
/// <term>Permissões</term>
/// </listheader>
/// <item>
/// <term>1</term>
/// <term>admin (ID 1)</term>
/// <term>Sem permissões explícitas (proprietário tem todas)</term>
/// </item>
/// <item>
/// <term>2</term>
/// <term>manager (ID 2)</term>
/// <term>manager pode editar; admin pode editar e excluir</term>
/// </item>
/// <item>
/// <term>3</term>
/// <term>user (ID 3)</term>
/// <term>user pode editar; admin pode editar e excluir</term>
/// </item>
/// <item>
/// <term>4</term>
/// <term>guest (ID 4)</term>
/// <term>admin pode editar e excluir</term>
/// </item>
/// </list>
/// <para>
/// <b>JUNIOR: Como usar estes dados para testar?</b>
/// Gere um token JWT para cada usuário e use nos headers Authorization.
/// O token deve conter a claim NameIdentifier com o ID do usuário.
/// </para>
/// </remarks>
async Task SeedDataAsync(ResourceDbContext context)
{
    // Limpa dados existentes (útil para reinicialização)
    context.Users.RemoveRange(context.Users);
    context.DocumentPermissions.RemoveRange(context.DocumentPermissions);
    context.Documents.RemoveRange(context.Documents);
    await context.SaveChangesAsync();

    // Cria usuários
    var admin = new AuthLabs.Shared.Models.User { Id = 1, UserName = "admin@authlabs.com", Email = "admin@authlabs.com" };
    var manager = new AuthLabs.Shared.Models.User { Id = 2, UserName = "manager@authlabs.com", Email = "manager@authlabs.com" };
    var user = new AuthLabs.Shared.Models.User { Id = 3, UserName = "user@authlabs.com", Email = "user@authlabs.com" };
    var guest = new AuthLabs.Shared.Models.User { Id = 4, UserName = "guest@authlabs.com", Email = "guest@authlabs.com" };

    context.Users.AddRange(admin, manager, user, guest);

    // JUNIOR: Doc1 - Pertence ao admin, sem permissões extras
    // O admin (proprietário) pode fazer tudo
    var doc1 = new Document
    {
        Id = 1,
        Title = "Relatório Financeiro Q1",
        Content = "Conteúdo do relatório financeiro...",
        OwnerId = "1", // admin
        Permissions = new List<DocumentPermission>()
    };

    // JUNIOR: Doc2 - Pertence ao manager
    // manager pode editar mas NÃO excluir
    // admin (que não é proprietário) pode editar E excluir
    var doc2 = new Document
    {
        Id = 2,
        Title = "Análise de Mercado",
        Content = "Conteúdo da análise de mercado...",
        OwnerId = "2", // manager
        Permissions = new List<DocumentPermission>
        {
            new() { Id = 1, DocumentId = 2, UserId = "2", CanEdit = true, CanDelete = false },
            new() { Id = 2, DocumentId = 2, UserId = "1", CanEdit = true, CanDelete = true }
        }
    };

    // JUNIOR: Doc3 - Pertence ao user
    // user pode editar mas NÃO excluir
    // admin pode editar e excluir
    var doc3 = new Document
    {
        Id = 3,
        Title = "Projeto Feature X",
        Content = "Conteúdo do projeto feature X...",
        OwnerId = "3", // user
        Permissions = new List<DocumentPermission>
        {
            new() { Id = 3, DocumentId = 3, UserId = "3", CanEdit = true, CanDelete = false },
            new() { Id = 4, DocumentId = 3, UserId = "1", CanEdit = true, CanDelete = true }
        }
    };

    // JUNIOR: Doc4 - Pertence ao guest
    // Apenas admin tem permissões (pode editar e excluir)
    // guest (proprietário) NÃO pode fazer nada além de ver?
    // Na verdade, proprietário SEMPRE pode fazer tudo - então guest pode editar e excluir
    var doc4 = new Document
    {
        Id = 4,
        Title = "Documentação API",
        Content = "Conteúdo da documentação da API...",
        OwnerId = "4", // guest
        Permissions = new List<DocumentPermission>
        {
            new() { Id = 5, DocumentId = 4, UserId = "1", CanEdit = true, CanDelete = true }
        }
    };

    context.Documents.AddRange(doc1, doc2, doc3, doc4);
    await context.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// JUNIOR: Ordem do middleware é importante!
// Authentication deve vir antes de Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Classe parcial vazia requerida para compile-time checks.
/// </summary>
/// <remarks>
/// JUNIOR: Esta classe existe apenas para que o compilador possa
/// verificar que tudo compila corretamente quando você usa
/// "dotnet build" ou "dotnet run" diretamente neste arquivo.
/// Em projetos web normais, o Program é gerado automaticamente
/// pelo template e esta classe não seria necessária.
/// </remarks>
public partial class Program { }
