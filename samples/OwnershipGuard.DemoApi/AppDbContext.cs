using Microsoft.EntityFrameworkCore;

namespace OwnershipGuard.DemoApi;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.OwnerId).HasMaxLength(64);
            e.Property(d => d.TenantId).HasMaxLength(64);
            e.Property(d => d.Title).HasMaxLength(256);
        });

        modelBuilder.Entity<Note>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.OwnerId).HasMaxLength(64);
            e.Property(n => n.Title).HasMaxLength(256);
        });
    }
}
