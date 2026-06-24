using AuthLabs.Shared.Data;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthLabs.Jwt.Tests.Integration;

public class JwtWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all database-related services to avoid conflicts
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.FullName?.Contains("DbContext", StringComparison.OrdinalIgnoreCase) == true ||
                            d.ServiceType.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
                            d.ServiceType.FullName?.Contains("Relational", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Also remove the IDbContextFactory if present
            var dbContextFactoryDescriptor = services
                .Where(d => d.ServiceType.Name.Contains("IDbContextFactory"))
                .ToList();
            foreach (var descriptor in dbContextFactoryDescriptor)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            var dbName = "TestDb_" + Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: dbName);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var host = base.CreateHost(builder);

        // Seed the database after the host is created
        SeedDatabase(host).GetAwaiter().GetResult();

        return host;
    }

    private async Task SeedDatabase(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        string[] roles = { "Admin", "Manager", "User", "Guest" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

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
}