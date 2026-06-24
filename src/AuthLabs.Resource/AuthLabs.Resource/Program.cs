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

// JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Database - InMemory for demonstration
builder.Services.AddDbContext<ResourceDbContext>(options =>
    options.UseInMemoryDatabase("AuthLabsResourceDb"));

// Authorization - Resource-based
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();

// Services
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

// Seed data / Dados iniciais com usuarios e documentos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ResourceDbContext>();
    await SeedDataAsync(context);
}

async Task SeedDataAsync(ResourceDbContext context)
{
    // Clear existing data
    context.Users.RemoveRange(context.Users);
    context.DocumentPermissions.RemoveRange(context.DocumentPermissions);
    context.Documents.RemoveRange(context.Documents);
    await context.SaveChangesAsync();

    // Create users - using Id as string for OwnerId compatibility
    var admin = new AuthLabs.Shared.Models.User { Id = 1, UserName = "admin@authlabs.com", Email = "admin@authlabs.com" };
    var manager = new AuthLabs.Shared.Models.User { Id = 2, UserName = "manager@authlabs.com", Email = "manager@authlabs.com" };
    var user = new AuthLabs.Shared.Models.User { Id = 3, UserName = "user@authlabs.com", Email = "user@authlabs.com" };
    var guest = new AuthLabs.Shared.Models.User { Id = 4, UserName = "guest@authlabs.com", Email = "guest@authlabs.com" };

    context.Users.AddRange(admin, manager, user, guest);

    // Create documents with permissions per seed data table
    var doc1 = new Document
    {
        Id = 1,
        Title = "Relatório Financeiro Q1",
        Content = "Conteúdo do relatório financeiro...",
        OwnerId = "1", // admin
        Permissions = new List<DocumentPermission>()
    };

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
