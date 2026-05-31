using Microsoft.EntityFrameworkCore;
using SocialService.Models;

namespace SocialService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Follow> Follows => Set<Follow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Follow>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.FollowerId, f.FollowedId }).IsUnique();
            e.HasIndex(f => f.FollowedId);
        });
    }
}