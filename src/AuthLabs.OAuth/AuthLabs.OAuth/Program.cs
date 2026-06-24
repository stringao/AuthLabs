using AuthLabs.Shared.Data;
using AuthLabs.Shared.Extensions;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// JUNIOR: Services são componentes reutilizáveis registrados no DI container
// O AddControllers() adiciona suporte a controllers MVC/Web API
builder.Services.AddControllers();
// AddEndpointsApiExplorer adiciona suporte a documentação Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
// AddSwaggerGen() adiciona geração automática de documentação Swagger
builder.Services.AddSwaggerGen();

// Database
// JUNIOR: Connection String - como conectar ao banco de dados
// Conexão com PostgreSQL local
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123";

// JUNIOR: AddSharedDbContext registra o DbContext no DI container
// DbContext é a "janela" do Entity Framework para o banco de dados
builder.Services.AddSharedDbContext(connectionString);

// Identity
// JUNIOR: ASP.NET Core Identity - sistema de autenticação e autorização
// Identity gerencia: usuários, senhas, roles, claims, login externo
//
// Dicionário de opções:
// options.Password.* = requisitos da senha
// - RequireDigit: precisa ter número
// - RequireLowercase: precisa ter letra minúscula
// - RequireUppercase: precisa ter letra maiúscula
// - RequireNonAlphanumeric: caracteres especiais (!@#$%...)
// - RequiredLength: tamanho mínimo
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;  // Permite senhas só com letras e números
    options.Password.RequiredLength = 6;               // Mínimo 6 caracteres
})
// AddEntityFrameworkStores: usa EF Core para persistir usuários e roles
.AddEntityFrameworkStores<AppDbContext>()
// AddDefaultTokenProviders: habilita tokens para reset de senha, 2FA, etc
.AddDefaultTokenProviders();

// OAuth Authentication - Google
// JUNIOR: AddAuthentication configura o SCHEMA de autenticação
// O DefaultScheme é o esquema padrão quando não especificamos um
builder.Services.AddAuthentication(options =>
{
    // IdentityConstants.ExternalScheme é o esquema para logins externos (OAuth)
    // Este é o padrão quando você usa login com Google, GitHub, etc
    options.DefaultScheme = IdentityConstants.ExternalScheme;
})
// .AddGoogle() adiciona o provider Google ao pipeline OAuth
.AddGoogle("Google", options =>
{
    // ClientId e ClientSecret vem do Console de Desenvolvedor Google
    // Configure em: https://console.cloud.google.com/apis/credentials
    options.ClientId = builder.Configuration["OAuth:Google:ClientId"] ?? "mock-client-id";
    options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "mock-client-secret";

    // CallbackPath: caminho URL que o Google redireciona após login
    // DEVE coincidir exatamente com o configurado no Google Console
    // Configurado no Google como: http://localhost:5001/signin-google
    options.CallbackPath = "/signin-google";

    // SaveTokens = true: salva access_token e refresh_token no cookie
    // Assim você pode usá-los depois para chamar APIs do Google
    // Sem isso, os tokens são descartados após a autenticação
    options.SaveTokens = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
// JUNIOR: Middleware Pipeline - ordem importa!
// O pipeline é executado na ordem que você escreve Use*()

// Em Development: habilita Swagger UI para testar a API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// JUNIOR: UseAuthentication() - adiciona middleware de autenticação
// Deve vir ANTES de UseAuthorization()
// Este middleware lê o cookie/token e.popula HttpContext.User
app.UseAuthentication();

// JUNIOR: UseAuthorization() - adiciona middleware de autorização
// Verifica se o usuário tem permissão para acessar o recurso
// Só funciona se UseAuthentication() veio antes!
// Ambos são necessários mesmo que você só use um.
app.UseAuthorization();

// Mapeia os controllers para rotas URL
// Ex: AuthController com [Route("api/[controller]")] responde em /api/auth
app.MapControllers();

// Seed data - criar usuários com logins externos
// JUNIOR: Seed é popular o banco com dados iniciais
// Executado uma vez quando a aplicação inicia
// Útil para criar admin inicial, roles, dados de teste
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    // Criar roles padrão da aplicação
    // JUNIOR: Roles são grupos de permissão
    // Exemplos:
    // - Admin: acesso total
    // - User: acesso básico
    // - Guest: acesso limitado
    //
    // Policies podem ser mais granulares que roles:
    // policy "CanManageUsers" pode permitir apenas adicionar usuários,
    // enquanto policy "CanDeleteUsers" permite apenas remover.
    string[] roles = { "Admin", "User", "Guest" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    // Criar usuário admin com login externo (demo)
    // JUNIOR: Este admin foi criado localmente (com senha)
    // Em produção, você normalmente:
    // 1. Desabilita registro público
    // 2. Cria admins via CLI ou painel admin
    // 3. Usa login OAuth para todos os usuários
    var adminEmail = "admin@authlabs.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new User { UserName = "admin", Email = adminEmail };
        // CreateAsync com senha faz hash da senha e salva no banco
        // Sem senha, seria um usuário sem senha (login só via OAuth)
        await userManager.CreateAsync(admin, "Admin123!");
        // Adiciona à role Admin - dando acesso total
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

// Inicia a aplicação
// JUNIOR: app.Run() inicia o servidor web
// O default é escutar em http://localhost:5000 ou 5001
app.Run();

// -----------------------------------------------------------
// RESUMO DO FLUXO OAuth NESTA APLICAÇÃO
// -----------------------------------------------------------
//
// 1. USUÁRIO acessa /api/auth/login/google
//    → AuthController.Login() é chamado
//    → Return Challenge() redireciona para Google
//
// 2. USUÁRIO faz login no Google
//    → Google mostra tela de consentimento
//    → Google redireciona para /signin-google?code=XXX
//
// 3. USUÁRIO chega em /api/auth/callback/google
//    → AuthController.Callback() é chamado
//    → Usa código para obter tokens do Google
//    → Cria/vincula usuário no Identity
//    → Cria cookie de sessão
//
// 4. USUÁRIO acessa /api/protected/secure
//    → [Authorize] verifica se está logado
//    → Se não: retorna 401
//    → Se sim: retorna dados
//
// -----------------------------------------------------------
// ONDE OS TOKENS SÃO ARMAZENADOS?
// -----------------------------------------------------------
//
// ACCESS TOKEN e REFRESH TOKEN:
// → São salvos no cookie de autenticação (因为 SaveTokens = true)
// → options.CookieManager.SetCookie() define o cookie
//
// INFORMAÇÕES DO USUÁRIO (Claims):
// → São codificadas no cookie de autenticação
// → Decodificadas automaticamente pelo UseAuthentication()
//
// Para acessar tokens no código:
// var result = await HttpContext.AuthenticateAsync();
// var accessToken = result.Properties?.Items[".Token.access_token"];
//
// -----------------------------------------------------------
