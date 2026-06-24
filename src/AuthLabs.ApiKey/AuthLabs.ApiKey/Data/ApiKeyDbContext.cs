using Microsoft.EntityFrameworkCore;

namespace AuthLabs.ApiKey.Data;

/// <summary>
/// DbContext proprio para API Keys.
/// </summary>
public class ApiKeyDbContext : DbContext
{
    public ApiKeyDbContext(DbContextOptions<ApiKeyDbContext> options) : base(options)
    {
    }

    public DbSet<Models.ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<Models.ApiKeyScope> ApiKeyScopes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Models.ApiKey>(entity =>
        {
            entity.HasIndex(k => k.Key).IsUnique();
            entity.Property(k => k.Key).IsRequired();
            entity.Property(k => k.ClientName).IsRequired();
        });

        builder.Entity<Models.ApiKeyScope>(entity =>
        {
            entity.HasIndex(s => new { s.ApiKeyId, s.Scope }).IsUnique();
            entity.HasOne(s => s.ApiKey)
                .WithMany(k => k.Scopes)
                .HasForeignKey(s => s.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
