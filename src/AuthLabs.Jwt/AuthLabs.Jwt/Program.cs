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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123";
builder.Services.AddSharedDbContext(connectionString);

// Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT
var jwtSettings = new JwtSettings
{
    SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "AuthLabs.Jwt",
    Audience = builder.Configuration["Jwt:Audience"] ?? "AuthLabs.Jwt.Api",
    AccessTokenExpirationMinutes = 15,
    RefreshTokenExpirationDays = 7
};
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed data - criar usuários de demonstração
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

    // Criar roles
    string[] roles = { "Admin", "Manager", "User", "Guest" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    // Criar usuários
    var users = new[]
    {
        (Email: "admin@authlabs.com", Password: "Admin123!", Role: "Admin"),
        (Email: "manager@authlabs.com", Password: "Manager123!", Role: "Manager"),
        (Email: "user@authlabs.com", Password: "User123!", Role: "User"),
        (Email: "guest@authlabs.com", Password: "Guest123!", Role: "Guest")
    };

    foreach (var (email, password, role) in users)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new User { UserName = email.Split('@')[0], Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

app.Run();