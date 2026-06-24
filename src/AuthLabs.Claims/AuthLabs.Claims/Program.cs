using AuthLabs.Claims.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using AuthPolicies = AuthLabs.Claims.Authorization.AuthorizationPolicies;
using CustomClaimHandler = AuthLabs.Claims.Authorization.CustomClaimHandler;

var builder = WebApplication.CreateBuilder(args);

// Adiciona serviços ao container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura autenticação por cookies.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login";
        options.Cookie.Name = "AuthLabs.Claims";
    });

// Configura políticas de autorização com claims.
builder.Services.AddAuthorization(options =>
{
    // Política para editar documentos: requer claim Document:Edit=true.
    options.AddPolicy(AuthPolicies.CanEditDocuments, policy =>
        policy.RequireClaim("Document:Edit", "true"));

    // Política para excluir documentos: requer claim Document:Delete=true.
    options.AddPolicy(AuthPolicies.CanDeleteDocuments, policy =>
        policy.RequireClaim("Document:Delete", "true"));

    // Política para gerenciar usuários: requer claim User:Manage=true.
    options.AddPolicy(AuthPolicies.CanManageUsers, policy =>
        policy.RequireClaim("User:Manage", "true"));

    // Política para usuários premium: requer claim Subscription:Tier=Premium.
    options.AddPolicy(AuthPolicies.IsPremiumUser, policy =>
        policy.RequireClaim("Subscription:Tier", "Premium"));
});

// Registra handlers e serviços.
builder.Services.AddScoped<IAuthorizationHandler, CustomClaimHandler>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();

var app = builder.Build();

// Configura o pipeline de requisições.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
