using AuthLabs.Shared.Data;
using AuthLabs.Shared.Extensions;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURAÇÃO DE SERVIÇOS (Dependency Injection - DI)
// ============================================================================
// JUNIOR: DI é um padrão onde o框架 (framework) injeta automaticamente
// as dependências que um objeto precisa, em vez do objeto criar elas mesmo.
// Isso facilita testes e reduz acoplamento entre componentes.

// Suporte a controllers (recebem e processam requests HTTP)
builder.Services.AddControllers();
// Permite que o Swagger descubra todos os endpoints automaticamente
builder.Services.AddEndpointsApiExplorer();
// Gera documentação Swagger (interface interativa para testar a API)
builder.Services.AddSwaggerGen();

// ============================================================================
// CONFIGURAÇÃO DO BANCO DE DADOS
// ============================================================================
// JUNIOR: Connection string = instructions para conectar no banco de dados
// Formato: Host=onde; Database=nome; Username=user; Password=senha
// O "??" significa: use o valor da esquerda se não for null, senão use o da direita

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123";

// Registrar o DbContext no container de DI
// AddSharedDbContext é uma extensão que configura o Entity Framework
// "Scoped" = uma nova instância é criada para cada request HTTP
builder.Services.AddSharedDbContext(connectionString);

// ============================================================================
// CONFIGURAÇÃO DO ASP.NET CORE IDENTITY
// ============================================================================
// JUNIOR: Identity é o sistema de autenticação/autorização do ASP.NET Core.
// Ele fornece:
// - Registro e login de usuários
// - Hash seguro de senhas (nunca guarda senha em texto puro!)
// - Gerenciamento de roles (Admin, User, Manager, etc)
// - Lockout (bloqueio após tentativas de login falhadas)
// - Suporte a autenticação de dois fatores (2FA)

// AddIdentity<Usuario, Role> configura o sistema completo
// <User, IdentityRole<int>> = usuário com ID inteiro, role com ID inteiro
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    // ----------------------------------------------------------
    // POLÍTICA DE SENHA
    // JUNIOR: Quanto mais restrições, mais segura a senha.
    // Mas se очень restritivo, usuários podem ter dificuldade.
    // Balanceie segurança com usabilidade!
    // ----------------------------------------------------------
    options.Password.RequireDigit = true;           // Deve conter número (0-9)
    options.Password.RequireLowercase = true;       // Deve conter letra minúscula
    options.Password.RequireUppercase = true;       // Deve conter letra maiúscula
    options.Password.RequireNonAlphanumeric = false; // NÃO precisa de caractere especial (@, #, etc)
    options.Password.RequiredLength = 6;            // Mínimo de 6 caracteres

    // NOTA: Em sistemas reais, use RequireNonAlphanumeric = true e RequiredLength >= 8
    // Exemplo de senha forte: Admin@123 (tem maiúscula, minúscula, número, símbolo)
})
// Dizer para o Identity usar Entity Framework como armazenamento
.AddEntityFrameworkStores<AppDbContext>()
// Adicionar providers de token (usados para: reset senha, confirmação email, etc)
.AddDefaultTokenProviders();

// ============================================================================
// CONFIGURAÇÃO DE AUTENTICAÇÃO POR COOKIE
// ============================================================================
// JUNIOR: Cookie Authentication é o método "tradicional" de autenticação web.
// Funciona assim:
// 1. Usuário faz login com email + senha
// 2. Servidor cria um "ticket" de autenticação e guarda no cookie
// 3. Cookie é enviado automaticamente em todas as requisições seguintes
// 4. Servidor lê o cookie e sabe quem é o usuário
//
// VANTAGENS DO COOKIE:
// - Funciona naturalmente com navegadores (cookies são automáticos)
// - O servidor mantém controle sobre a sessão (pode revogar!)
// - Funciona bem para aplicações onde o frontend é server-rendered
//
// DESVANTAGENS:
// - Só funciona para clientes que aceitam cookies (quase todos)
// - Cookie pode ser roubado (por isso HttpOnly e SameSite são importantes!)

// Definir qual esquema de autenticação usar
builder.Services.AddAuthentication(options =>
{
    // CookieAuthenticationDefaults.AuthenticationScheme = "Cookies"
    // Isso diz: "use autenticação por cookie como padrão"
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    // ----------------------------------------------------------
    // CONFIGURAÇÕES DO COOKIE
    // JUNIOR: Estas opções são CRUCIAIS para segurança!
    // ----------------------------------------------------------

    // Nome do cookie no navegador
    // JUNIOR: Cookies têm nome para identificar cada um
    options.Cookie.Name = "AuthLabs.Cookie";

    // HttpOnly = true = JavaScript NÃO pode ler este cookie
    // JUNIOR: Isso previne ataques XSS (Cross-Site Scripting).
    // Se um atacante injetar JavaScript malicioso na página,
    // ele não conseguirá roubar o cookie de autenticação.
    options.Cookie.HttpOnly = true;

    // SameSite = Strict = Cookie só é enviado em requisições do MESMO site
    // JUNIOR: Isso previne ataques CSRF (Cross-Site Request Forgery).
    // Se alguém tentar fazer um formulário em outro site que envia requisição
    // para aqui, o cookie NÃO será enviado.
    options.Cookie.SameSite = SameSiteMode.Strict;

    // Tempo que o cookie fica válido
    // JUNIOR: 20 minutos de inatividade = o usuário é deslogado automaticamente.
    // Isso é bom para segurança - se esquecer o computador ligado, ninguém usa.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);

    // SlidingExpiration = true = O tempo ZERA quando o usuário interage
    // JUNIOR: Se usuário está ativo, o cookie RENOVA automaticamente.
    // Isso evita que usuário seja deslogado no meio de uma atividade.
    // Se false, o cookie expira no tempo exato, independente de atividade.
    options.SlidingExpiration = true;

    // Paths para login, logout e acesso negado
    // JUNIOR: Estes são os "redirects" automáticos do Identity.
    // Se usuário não autenticado tenta acessar página protegida,
    // é redirecionado para LoginPath.
    options.LoginPath = "/api/auth/login";
    options.LogoutPath = "/api/auth/logout";
    options.AccessDeniedPath = "/api/auth/access-denied";
});

// Configurar autorização (permissões de acesso)
builder.Services.AddAuthorization();

var app = builder.Build();

// ============================================================================
// PIPELINE DE REQUISIÇÃO HTTP (Middleware)
// ============================================================================
// JUNIOR: Pipeline é uma sequência de "filtros" que processam cada request.
// Cada middleware pode: processar, modificar, rejeitar, ou passar adiante.
// A ORDEM dos Use*() é muito importante!

// Só mostrar Swagger em development (não em produção!)
if (app.Environment.IsDevelopment())
{
    // Swagger = documentação interativa da API no browser
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ATENÇÃO: ORDEM CRÍTICA!
// 1. UseAuthentication() = extrai identidade do usuário (do cookie)
// 2. UseAuthorization() = verifica permissões baseadas na identidade
// 3. MapControllers() = executa o controller correto

app.UseAuthentication(); // "Quem você é?"
app.UseAuthorization();  // "O que você pode fazer?"

app.MapControllers();    // Processar request

// ============================================================================
// SEED DATA - Criar usuários iniciais
// ============================================================================
// JUNIOR: Seed data = dados que são criados automaticamente no primeiro start.
// Usamos para criar os 4 usuários de demonstração.

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    // ----------------------------------------------------------
    // PASSO 1: Criar as roles
    // ----------------------------------------------------------
    string[] roles = { "Admin", "Manager", "User", "Guest" };

    foreach (var role in roles)
    {
        // Só criar se não existir
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    // ----------------------------------------------------------
    // PASSO 2: Criar os 4 usuários de demonstração
    // ----------------------------------------------------------
    // JUNIOR: Estos usuários servem para você testar a API.
    // Você pode fazer login com qualquer um deles.
    //
    // DIFERENÇA ENTRE AS ROLES:
    // - Admin: acesso total, pode tudo
    // - Manager: gerencia usuários, vê relatórios
    // - User: acesso básico a recursos
    // - Guest: acesso apenas leitura (se configurado)
    var users = new[]
    {
        (Email: "admin@authlabs.com", Password: "Admin123!", Role: "Admin"),
        (Email: "manager@authlabs.com", Password: "Manager123!", Role: "Manager"),
        (Email: "user@authlabs.com", Password: "User123!", Role: "User"),
        (Email: "guest@authlabs.com", Password: "Guest123!", Role: "Guest")
    };

    foreach (var (email, password, role) in users)
    {
        // Verificar se usuário já existe antes de criar
        if (await userManager.FindByEmailAsync(email) == null)
        {
            // UserName = parte antes do @ no email
            var user = new User { UserName = email.Split('@')[0], Email = email };

            // CreateAsync: cria usuário E faz hash da senha
            // JUNIOR: O Identity NUNCA guarda senha em texto puro!
            // Ele usa BCrypt ou similar para criar um hash irreversível.
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Adicionar usuário à role
                // JUNIOR: Um usuário pode ter múltiplas roles se precisar!
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

// Iniciar o servidor
app.Run();