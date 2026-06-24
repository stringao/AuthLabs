using AuthLabs.Claims.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using AuthPolicies = AuthLabs.Claims.Authorization.AuthorizationPolicies;
using CustomClaimHandler = AuthLabs.Claims.Authorization.CustomClaimHandler;

var builder = WebApplication.CreateBuilder(args);

// Adiciona serviços ao container de injeção de dependência.
// JUNIOR: O que é injeção de dependência (DI)?
// É um padrão de design onde as dependências (serviços) são "injetadas" nas classes
// que precisam delas, em vez de serem criadas diretamente.
// O container de DI do .NET gerencia o ciclo de vida desses serviços.
//
// AddControllers() - adiciona suporte a controllers MVC/API
// AddEndpointsApiExplorer() - adiciona suporte à exploração de APIs (útil para Swagger)
// AddSwaggerGen() - adiciona suporte à geração de documentação Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura autenticação por cookies.
// JUNIOR: O que é autenticação por cookies?
// É um esquema de autenticação onde, após o usuário fazer login,
// suas credenciais são armazenadas em um cookie criptografado no navegador.
// Em cada requisição subsequente, o navegador envia automaticamente o cookie,
// e o servidor valida as credenciais sem pedir login novamente.
//
// CookieAuthenticationDefaults.AuthenticationScheme é o nome padrão do esquema
// de autenticação por cookies ("Cookies").
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // JUNIOR: LoginPath é a URL para onde o usuário é redirecionado
        // quando tenta acessar um recurso protegido sem estar autenticado.
        // Por exemplo: se /api/protected/edit requer autenticação e o usuário
        // não está logado, ele será redirecionado para /api/auth/login.
        options.LoginPath = "/api/auth/login";

        // JUNIOR: Cookie.Name define o nome do cookie que será armazenado
        // no navegador do usuário. Este nome é usado para identificar nosso
        // cookie de autenticação entre todos os cookies do site.
        options.Cookie.Name = "AuthLabs.Claims";
    });

// Configura políticas de autorização com claims.
// JUNIOR: O que são políticas de autorização?
// Uma política de autorização é um nome amigável para um conjunto de requisitos.
// Em vez de escrever "requer claim Document:Edit=true" em cada endpoint,
// definimos uma política chamada "CanEditDocuments" e referenciamos ela.
//
// Benefits das políticas:
// 1. Centralização: requisitos definidos em um único lugar
// 2. Reutilização: mesma política em múltiplos endpoints
// 3. Clareza: nomes descritivos são mais legíveis
// 4. Manutenção: mudanças afetam todos os endpoints automaticamente
//
// O método AddAuthorization() configura o sistema de autorização.
// A propriedade options é do tipo AuthorizationOptions.
builder.Services.AddAuthorization(options =>
{
    // JUNIOR: AddPolicy() adiciona uma nova política ao sistema.
    // Parâmetros:
    // - Primeiro parâmetro (string): nome único da política
    // - Segundo parâmetro (Action): configuração da política com os requisitos

    // Política para editar documentos: requer claim Document:Edit=true.
    // Se um usuário tenta acessar um endpoint com [Authorize(Policy = "CanEditDocuments")]
    // e ele NÃO tem o claim "Document:Edit" com valor "true", o acesso é negado.
    options.AddPolicy(AuthPolicies.CanEditDocuments, policy =>
        policy.RequireClaim("Document:Edit", "true"));

    // Política para excluir documentos: requer claim Document:Delete=true.
    // Esta é uma permissão SEPARADA de editar - um usuário pode editar mas não excluir.
    options.AddPolicy(AuthPolicies.CanDeleteDocuments, policy =>
        policy.RequireClaim("Document:Delete", "true"));

    // Política para gerenciar usuários: requer claim User:Manage=true.
    // Esta política é para funcionalidades administrativas.
    options.AddPolicy(AuthPolicies.CanManageUsers, policy =>
        policy.RequireClaim("User:Manage", "true"));

    // Política para usuários premium: requer claim Subscription:Tier=Premium.
    // Exemplo de política baseada em nível/tier de assinatura.
    options.AddPolicy(AuthPolicies.IsPremiumUser, policy =>
        policy.RequireClaim("Subscription:Tier", "Premium"));
});

// Registra handlers e serviços no container de DI.
// JUNIOR: Por que registrar?
// Para que o sistema de injeção de dependência saiba como criar
// instâncias dessas classes quando elas forem necessárias.
//
// AddScoped significa que uma nova instância é criada para cada requisição HTTP.
// Isso é apropriado para handlers de autorização porque eles precisam
// acessar dados do request atual.
//
// AddSingleton criaria uma única instância para toda a aplicação.
// AddTransient criaria uma nova instância cada vez que é solicitada.
builder.Services.AddScoped<IAuthorizationHandler, CustomClaimHandler>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
// JUNIOR: O pipeline é executado em ordem - cada middleware vê a requisição
// antes de passar para o próximo. A ordem IMPORTA!

// Apenas no ambiente de desenvolvimento: habilita Swagger para documentação da API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// JUNIOR: UseAuthentication() deve vir ANTES de UseAuthorization()!
// Este middleware lê e valida o cookie de autenticação, definindo
// context.User com as claims do usuário logado.
app.UseAuthentication();

// JUNIOR: UseAuthorization() verifica se o usuário tem permissão
// para acessar endpoints protegidos com [Authorize].
// Ele verifica as políticas definidas, mas NÃO bloqueia requisições
// por conta própria - isso é feito pelo atributo [Authorize].
app.UseAuthorization();

// Mapeia os controllers para que suas rotas fiquem disponíveis.
// JUNIOR: MapControllers() registra todas as rotas definidas nos controllers.
// Por exemplo, o ProtectedController com [Route("api/[controller]")]
// terá suas rotas disponíveis em /api/Protected.
app.MapControllers();

app.Run();

// JUNIOR: Esta classe partial existe apenas para que o compilador
// possa gerar uma classe Program com método Main().
// Não é necessário entender isso agora - é só uma convenção do .NET 6+.
public partial class Program { }
