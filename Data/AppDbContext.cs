using Microsoft.EntityFrameworkCore;
using mvc7.Models;

namespace mvc7.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Username là duy nhất
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.AccessFailedCount).HasDefaultValue(0);
            entity.Property(u => u.IsLocked).HasDefaultValue(false);
        });
    }
}
