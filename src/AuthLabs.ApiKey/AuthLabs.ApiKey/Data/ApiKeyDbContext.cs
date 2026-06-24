using AuthLabs.ApiKey.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthLabs.ApiKey.Data;

/// <summary>
/// DbContext proprio para API Keys - estende o contexto base.
/// </summary>
public class ApiKeyDbContext : DbContext
{
    public ApiKeyDbContext(DbContextOptions<ApiKeyDbContext> options) : base(options)
    {
    }

    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<ApiKeyScope> ApiKeyScopes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(k => k.Key).IsUnique();
            entity.Property(k => k.Key).IsRequired();
            entity.Property(k => k.ClientName).IsRequired();
        });

        builder.Entity<ApiKeyScope>(entity =>
        {
            entity.HasIndex(s => new { s.ApiKeyId, s.Scope }).IsUnique();
            entity.HasOne(s => s.ApiKey)
                .WithMany(k => k.Scopes)
                .HasForeignKey(s => s.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
