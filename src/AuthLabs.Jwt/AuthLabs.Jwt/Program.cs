using System.Text;
using AuthLabs.Jwt.Services;
using AuthLabs.Shared.Data;
using AuthLabs.Shared.Extensions;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURAÇÃO DE SERVIÇOS (Dependency Injection)
// ============================================================================
// JUNIOR: Aqui registramos todos os serviços que a aplicação usa.
// Cada call "AddX" registra um serviço no container de DI.
// Quando um controller precisa de um serviço, o ASP.NET injeta automaticamente.

// Adicionar suporte a controllers e API endpoints
// JUNIOR: AddControllers() registra serviços para handling de requests HTTP.
builder.Services.AddControllers();

// ============================================================================
// CONFIGURAÇÃO DO BANCO DE DADOS
// ============================================================================
// JUNIOR: Connection string é a "receita" para conectar no banco:
// Host = onde o banco está rodando (localhost = sua máquina)
// Database = nome do banco (authlabs)
// Username/Password = credenciais de acesso

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123";

// Registrar o DbContext no container de DI
// JUNIOR: AddSharedDbContext é uma extensão que faz o trabalho de:
// 1. Criar DbContextOptions com a connection string
// 2. Registrar AppDbContext como Scoped service
// "Scoped" = uma instância por requisição HTTP
builder.Services.AddSharedDbContext(connectionString);

// ============================================================================
// CONFIGURAÇÃO DO ASP.NET CORE IDENTITY
// ============================================================================
// JUNIOR: Identity é o sistema de autenticação/autorização do ASP.NET Core.
// Ele gerencia:
// - Criação e autenticação de usuários
// - Hash de senhas (seguro!)
// - Roles e claims
// - Lockout (bloqueio após tentativas falhas)

// AddIdentity configura o sistema completo de Identity
// Parâmetros genéricos: <User, Role> onde User=classe de usuário, Role=classe de role
// IdentityRole<int> = role com ID inteiro (nós definimos assim)
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    // Configuração de senha - quanto mais restritivo, mais seguro
    // JUNIOR: Estas opções definem REGRAS para senhas dos usuários:
    options.Password.RequireDigit = true;           // Precisa ter número
    options.Password.RequireLowercase = true;       // Precisa ter letra minúscula
    options.Password.RequireUppercase = true;       // Precisa ter letra maiúscula
    options.Password.RequireNonAlphanumeric = false; // Não precisa de caractere especial
    options.Password.RequiredLength = 6;            // Mínimo 6 caracteres

    // NOTA: Em produção, use RequireNonAlphanumeric = true e RequiredLength >= 8
})
// Dizer para o Identity usar Entity Framework como storage
.AddEntityFrameworkStores<AppDbContext>()
// Adicionar "token providers" padrão (para reset de senha, email confirmation, etc)
.AddDefaultTokenProviders();

// ============================================================================
// CONFIGURAÇÃO DO JWT
// ============================================================================
// JUNIOR: Aqui configuramos o serviço de JWT - criação e validação de tokens.

// Criar objeto de configurações com valores do appsettings.json
// O "??" é o "null coalescing operator" - se o valor à esquerda for null,
// usa o valor à direita como padrão.
var jwtSettings = new JwtSettings
{
    SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "AuthLabs.Jwt",
    Audience = builder.Configuration["Jwt:Audience"] ?? "AuthLabs.Jwt.Api",
    AccessTokenExpirationMinutes = 15,
    RefreshTokenExpirationDays = 7
};

// Registrar como SINGLETON = uma instância para toda a aplicação
// JUNIOR: Singleton é criado uma vez e reuse em toda a aplicação.
// Faz sentido para JwtSettings porque não muda durante a execução.
builder.Services.AddSingleton(jwtSettings);

// Registrar serviços de JWT
// Scoped = uma instância por requisição HTTP (garante thread-safety)
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ============================================================================
// CONFIGURAÇÃO DO MIDDLEWARE DE AUTENTICAÇÃO JWT
// ============================================================================
// JUNIOR: Authentication middleware "intercepta" requests HTTP e:
// 1. Extrai o token JWT do header Authorization
// 2. Valida o token
// 3. Cria uma "identidade" para o usuário e adiciona ao HttpContext

builder.Services.AddAuthentication(options =>
{
    // Definir qual esquema de autenticação usar por padrão
    // JWT Bearer é o padrão para APIs que usam tokens JWT
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configurar como VALIDAR tokens JWT que chegam nas requisições
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Verificar se o token foi assinado com nossa chave secreta
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

        // Verificar se o emissor do token é o esperado
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,

        // Verificar se o token foi emitido para nós
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        // Verificar se o token ainda está dentro do prazo de validade
        ValidateLifetime = true,

        // Sem tolerância de tempo - se expirou, expirou
        // JUNIOR: ClockSkew é uma margem de tolerância para tempo.
        // 0 = sem tolerância (exatamente o tempo de expiração)
        ClockSkew = TimeSpan.Zero
    };
});

// Configurar autorização - quem pode acessar o quê
builder.Services.AddAuthorization();

var app = builder.Build();

// ============================================================================
// CONFIGURAÇÃO DO PIPELINE DE REQUISIÇÃO HTTP
// ============================================================================
// JUNIOR: O pipeline é uma SEQUÊNCIA de middleware que processa cada request.
// Cada middleware pode: processar, modificar, ou rejeitar a request.
// A ordem IMPORTA - middleware registrado primeiro executa primeiro.

// ATENÇÃO: Ordem importante!
// 1. UseAuthentication() deve vir ANTES de UseAuthorization()
// 2. UseAuthorization() vem antes de MapControllers()

// Autenticação = "quem você é?"
app.UseAuthentication();

// Autorização = "o que você pode fazer?"
app.UseAuthorization();

// Mapear controllers para endpoints
// JUNIOR: MapControllers() conecta seus controllers às rotas da URL.
// Ex: AuthController com [Route("api/[controller]")] responde em /api/auth
app.MapControllers();

// ============================================================================
// SEED DATA - Criar usuários de demonstração
// ============================================================================
// JUNIOR: Seed data é "dados iniciais" criados quando a aplicação inicia.
// Usamos isso para criar os usuários de demonstração automáticamente.

// Escopo é necessário porque UserManager e RoleManager são Scoped
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    // ----------------------------------------------------------
    // PASSO 1: Criar as roles se não existirem
    // ----------------------------------------------------------
    // Roles são "papéis" que os usuários podem ter.
    // Cada role pode ter permissões diferentes.
    string[] roles = { "Admin", "Manager", "User", "Guest" };

    foreach (var role in roles)
    {
        // RoleExistsAsync verifica se a role já existe no banco
        if (!await roleManager.RoleExistsAsync(role))
        {
            // Criar nova role
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    // ----------------------------------------------------------
    // PASSO 2: Criar usuários de demonstração
    // ----------------------------------------------------------
    // Usuários de demonstração para testar a API:
    // - admin@authlabs.com / Admin123! (role: Admin)
    // - manager@authlabs.com / Manager123! (role: Manager)
    // - user@authlabs.com / User123! (role: User)
    // - guest@authlabs.com / Guest123! (role: Guest)

    var users = new[]
    {
        (Email: "admin@authlabs.com", Password: "Admin123!", Role: "Admin"),
        (Email: "manager@authlabs.com", Password: "Manager123!", Role: "Manager"),
        (Email: "user@authlabs.com", Password: "User123!", Role: "User"),
        (Email: "guest@authlabs.com", Password: "Guest123!", Role: "Guest")
    };

    foreach (var (email, password, role) in users)
    {
        // Verificar se usuário já existe (não recriar se já existir)
        if (await userManager.FindByEmailAsync(email) == null)
        {
            // Criar usuário
            // JUNIOR: UserName é separado do Email em Identity.
            // Aqui usamos a parte antes do @ como UserName.
            var user = new User { UserName = email.Split('@')[0], Email = email };

            // CreateAsync faz:
            // 1. Hashear a senha (nunca guarda senha em plain text!)
            // 2. Salvar usuário no banco
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Adicionar usuário à role
                // JUNIOR: Um usuário pode ter MÚLTIPLAS roles.
                // Ex: um usuário pode ser "Admin" E "Manager" ao mesmo tempo.
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

// Iniciar a aplicação
// JUNIOR: app.Run() é o ponto final - inicia o servidor HTTP.
// A aplicação fica rodando até ser encerrada (Ctrl+C ou kill).
app.Run();