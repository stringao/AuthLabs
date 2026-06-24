/*
 * JUNIOR: AuthLabs.Rbac - Sistema de Autorização Baseada em Papéis (RBAC)
 * ======================================================================
 *
 * Este projeto demonstra dois tipos de autorização no ASP.NET Core:
 *
 * 1. RBAC (Role-Based Access Control / Controle de Acesso Baseado em Papéis)
 *    - Usa "roles" (papéis) como "Admin", "Manager", "User", "Guest"
 *    - O usuário pertence a um ou mais papéis
 *    - A autorização verifica se o usuário TEM ou NÃO TEM um papel específico
 *    - Exemplo: [Authorize(Roles = "Admin")] permite apenas Admin
 *
 * 2. Claims-Based (Baseado em Claims / Declarações)
 *    - Claims são pares de chave-valor que descrevem características do usuário
 *    - Exemplo: ClaimTypes.Email = "usuario@email.com"
 *    - Exemplo: ClaimTypes.Role = "Admin"
 *    - Claims são mais granulares e flexíveis que roles
 *    - Claims podem representar: data de nascimento, departamento, nível de acesso, etc.
 *
 * DIFERENÇA PRINCIPAL:
 * - Roles: "você É um Admin" (binário: sim ou não)
 * - Claims: "você TEM permissão para acessar relatórios financeiros" (mais específico)
 *
 * Neste projeto usamos BOTH (papéis E claims coexistem):
 * - Roles são armazenadas como Claims automaticamente pelo Identity
 * - Cada role vira uma Claim com Type = ClaimTypes.Role e Value = nome da role
 */

using AuthLabs.Rbac.Services;
using AuthLabs.Shared.Data;
using AuthLabs.Shared.Extensions;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// JUNIOR: AddControllers() registra todos os controllers da aplicação
// Isso permite que o ASP.NET Core processe requests HTTP para nossos endpoints
builder.Services.AddControllers();

// JUNIOR: AddEndpointsApiExplorer e AddSwaggerGen são para documentação automática da API
// O Swagger mostra todos os endpoints disponíveis e permite testar direto do browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
// Configuração do Banco de Dados
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123";

// JUNIOR: AddSharedDbContext registra o DbContext do Entity Framework
// O EF Core usa isso para conectar ao banco de dados e fazer operações CRUD
builder.Services.AddSharedDbContext(connectionString);

// ============================================================
// Identity - Sistema de Autenticação e Autorização
// ============================================================

// JUNIOR: AddIdentity configura o sistema de identidade do ASP.NET Core
// Ele gerencia:
//   - Criação de usuários
//   - Hash de senhas (nunca armazenamos senhas em texto puro!)
//   - Login/logout
//   - Roles (papeis)
//   - Lockout (bloqueio após tentativas falhas)
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    // JUNIOR: Configurações de senha - boas práticas de segurança:
    options.Password.RequireDigit = true;           // Precisa ter número
    options.Password.RequireLowercase = true;       // Precisa ter letra minúscula
    options.Password.RequireUppercase = true;       // Precisa ter letra maiúscula
    options.Password.RequireNonAlphanumeric = false; // NÃO precisa de caractere especial (@, #, etc)
    options.Password.RequiredLength = 6;            // Mínimo de 6 caracteres
})
.AddEntityFrameworkStores<AppDbContext>()  // Usa EF Core para persistir dados do Identity
.AddDefaultTokenProviders();               // Fornece tokens para reset de senha, email, etc.

// ============================================================
// Cookie Authentication
// ============================================================

// JUNIOR: Authentication é o processo de IDENTIFICAR quem você é (login)
// Authorization é o processo de VERIFICAR o que você pode fazer (permissões)

// Aqui configuramos autenticação por COOKIE (não por JWT token)
// Cookie é mais simples para apps web tradicionais (MVC, Razor Pages)
// JWT é melhor para APIs que alimentam apps mobile ou SPAs (React, Vue, Angular)
builder.Services.AddAuthentication()
.AddCookie(options =>
{
    // JUNIOR: Nome do cookie que armazenará a sessão do usuário
    options.Cookie.Name = "AuthLabs.Rbac";

    // JUNIOR: Paths de redirect quando não autenticado ou acesso negado
    options.LoginPath = "/api/auth/login";         // Redirect para login
    options.LogoutPath = "/api/auth/logout";        // Redirect após logout
    options.AccessDeniedPath = "/api/auth/access-denied"; // Redirect se não tem permissão

    // JUNIOR: Cookie expira após 24 horas - usuário precisa fazer login novamente
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

// JUNIOR: Authorization verifica se o usuário TEM PERMISSÃO para acessar um recurso
// Sem isso, mesmo logado, qualquer um poderia acessar qualquer endpoint
builder.Services.AddAuthorization();

// ============================================================
// Services - Injeção de Dependência
// ============================================================

// JUNIOR: Scoped significa que uma nova instância é criada por REQUEST HTTP
// Isso é bom para serviços que guardam estado do usuário atual
builder.Services.AddScoped<IRoleService, RoleService>();

var app = builder.Build();

// ============================================================
// Configuração do Pipeline de Requisições HTTP
// ============================================================

// JUNIOR: O pipeline é uma sequência de componentes que processam cada request
// A ordem IMPORTA! Authentication vem ANTES de Authorization

if (app.Environment.IsDevelopment())
{
    // JUNIOR: Swagger só aparece em development - nunca em produção!
    app.UseSwagger();
    app.UseSwaggerUI();
}

// JUNIOR: UseAuthentication lê o cookie e identifica o usuário
// Preenche HttpContext.User com as informações do usuário logado
app.UseAuthentication();

// JUNIOR: UseAuthorization verifica se o usuário tem permissão (roles/claims)
// Só é atingido se UseAuthentication passou (usuário identificado)
app.UseAuthorization();

// JUNIOR: MapControllers cria as rotas para todos os controllers
// Ex: AuthController com [Route("api/[controller]")] responde em /api/auth
app.MapControllers();

// ============================================================
// Seed Data - Criando dados iniciais
// ============================================================

// JUNIOR: Seed é popular o banco com dados iniciais (usuários e roles de demo)
// Isso roda automaticamente na primeira vez que a aplicação inicia

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    // JUNIOR: Criar as 4 roles do sistema
    // Cada role representa um nível de acesso diferente:
    // - Admin: acesso TOTAL (pode fazer tudo)
    // - Manager: acesso a relatórios e gerenciamento
    // - User: usuário comum com acesso básico
    // - Guest: acesso mínimo/público
    string[] roles = { "Admin", "Manager", "User", "Guest" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    // JUNIOR: Criar usuários de demonstração - cada um com uma role diferente
    // SENHAS: Note que todas seguem o padrão (ex: Admin123!)
    // Em produção NUNCA use senhas assim - use senhas aleatórias!
    var users = new[]
    {
        (Email: "admin@authlabs.com", Password: "Admin123!", Role: "Admin"),
        (Email: "manager@authlabs.com", Password: "Manager123!", Role: "Manager"),
        (Email: "user@authlabs.com", Password: "User123!", Role: "User"),
        (Email: "guest@authlabs.com", Password: "Guest123!", Role: "Guest")
    };

    // JUNIOR: Para cada usuário, verificamos se já existe antes de criar
    // Isso evita duplicar usuários se a aplicação reiniciar
    foreach (var (email, password, role) in users)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new User { UserName = email.Split('@')[0], Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // JUNIOR: AddToRoleAsync associa o usuário à role
                // Isso cria uma Claim自动omaticamente: ClaimTypes.Role = "Admin" (ou outra role)
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

app.Run();
