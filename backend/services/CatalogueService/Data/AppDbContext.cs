using CatalogueService.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CatalogueService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Movie> Movies => Set<Movie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).HasMaxLength(300).IsRequired();
            e.Property(m => m.TitleRu).HasMaxLength(300);
            e.Property(m => m.Director).HasMaxLength(200).IsRequired();
            e.Property(m => m.Genre).HasMaxLength(100).IsRequired();
            e.HasIndex(m => m.Title);
            e.HasIndex(m => m.TitleRu);
        });
    }
}
