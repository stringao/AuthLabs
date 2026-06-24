using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using AuthLabs.Windows.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura autenticacao Windows (Negotiate - Kerberos/NTLM)
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireWindowsAuth", policy =>
    {
        policy.AuthenticationSchemes.Add(NegotiateDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add(NegotiateDefaults.AuthenticationScheme);
        policy.RequireRole("Admin");
    });
});

// Registra servicos
builder.Services.AddScoped<IWindowsAuthService, WindowsAuthService>();

// Transforma claims do Windows/AD em roles da aplicacao
builder.Services.AddSingleton<IClaimsTransformation, WindowsClaimsTransformer>();

builder.Services.AddControllers();

var app = builder.Build();

// Configura pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de health check
app.MapGet("/", () => Results.Ok(new
{
    service = "AuthLabs.Windows",
    authentication = "Windows Authentication (Negotiate/Kerberos/NTLM)",
    status = "running"
}));

app.Run();
