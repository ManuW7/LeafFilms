using ActivityService.Models;
using Microsoft.EntityFrameworkCore;

namespace ActivityService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WatchHistory> WatchHistory => Set<WatchHistory>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistMovie> PlaylistMovies => Set<PlaylistMovie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WatchHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<WatchlistItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();
        });

        modelBuilder.Entity<Playlist>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Movies).WithOne(x => x.Playlist).HasForeignKey(x => x.PlaylistId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlaylistMovie>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlaylistId, x.MovieId }).IsUnique();
        });
    }
}