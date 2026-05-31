using Microsoft.EntityFrameworkCore;
using ReviewService.Models;

namespace ReviewService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.MovieId);
            e.HasIndex(r => r.UserId);
            // Один пользователь — один отзыв на фильм
            e.HasIndex(r => new { r.UserId, r.MovieId }).IsUnique();
            e.Property(r => r.Text).HasMaxLength(2000);
        });
    }
}