using ActivityService.Data;
using ActivityService.Models;
using Microsoft.EntityFrameworkCore;

namespace ActivityService.Repositories;

// ── Watch History ─────────────────────────────────────────────────────────────

public interface IWatchHistoryRepository
{
    Task<IEnumerable<WatchHistory>> GetByUserAsync(Guid userId);
    Task<WatchHistory> AddAsync(WatchHistory entry);
}

public class WatchHistoryRepository : IWatchHistoryRepository
{
    private readonly AppDbContext _db;
    public WatchHistoryRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<WatchHistory>> GetByUserAsync(Guid userId)
        => await _db.WatchHistory.AsNoTracking()
               .Where(x => x.UserId == userId)
               .OrderByDescending(x => x.WatchedAt)
               .ToListAsync();

    public async Task<WatchHistory> AddAsync(WatchHistory entry)
    { _db.WatchHistory.Add(entry); await _db.SaveChangesAsync(); return entry; }
}

// ── Watchlist ─────────────────────────────────────────────────────────────────

public interface IWatchlistRepository
{
    Task<IEnumerable<WatchlistItem>> GetByUserAsync(Guid userId);
    Task<WatchlistItem?> GetAsync(Guid userId, Guid movieId);
    Task<WatchlistItem> AddAsync(WatchlistItem item);
    Task RemoveAsync(Guid userId, Guid movieId);
}

public class WatchlistRepository : IWatchlistRepository
{
    private readonly AppDbContext _db;
    public WatchlistRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<WatchlistItem>> GetByUserAsync(Guid userId)
        => await _db.WatchlistItems.AsNoTracking()
               .Where(x => x.UserId == userId)
               .OrderByDescending(x => x.AddedAt)
               .ToListAsync();

    public async Task<WatchlistItem?> GetAsync(Guid userId, Guid movieId)
        => await _db.WatchlistItems.AsNoTracking()
               .FirstOrDefaultAsync(x => x.UserId == userId && x.MovieId == movieId);

    public async Task<WatchlistItem> AddAsync(WatchlistItem item)
    { _db.WatchlistItems.Add(item); await _db.SaveChangesAsync(); return item; }

    public async Task RemoveAsync(Guid userId, Guid movieId)
    {
        var item = await _db.WatchlistItems.FirstOrDefaultAsync(x => x.UserId == userId && x.MovieId == movieId);
        if (item is not null) { _db.WatchlistItems.Remove(item); await _db.SaveChangesAsync(); }
    }
}

// ── Playlists ─────────────────────────────────────────────────────────────────

public interface IPlaylistRepository
{
    Task<IEnumerable<Playlist>> GetByUserAsync(Guid userId);
    Task<Playlist?> GetByIdAsync(Guid id);
    Task<Playlist> CreateAsync(Playlist playlist);
    Task DeleteAsync(Guid id);
    Task AddMovieAsync(PlaylistMovie movie);
    Task RemoveMovieAsync(Guid playlistId, Guid movieId);
}

public class PlaylistRepository : IPlaylistRepository
{
    private readonly AppDbContext _db;
    public PlaylistRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Playlist>> GetByUserAsync(Guid userId)
        => await _db.Playlists.AsNoTracking()
               .Include(p => p.Movies)
               .Where(p => p.UserId == userId)
               .ToListAsync();

    public async Task<Playlist?> GetByIdAsync(Guid id)
        => await _db.Playlists.Include(p => p.Movies).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Playlist> CreateAsync(Playlist playlist)
    { _db.Playlists.Add(playlist); await _db.SaveChangesAsync(); return playlist; }

    public async Task DeleteAsync(Guid id)
    {
        var p = await _db.Playlists.FindAsync(id);
        if (p is not null) { _db.Playlists.Remove(p); await _db.SaveChangesAsync(); }
    }

    public async Task AddMovieAsync(PlaylistMovie movie)
    { _db.PlaylistMovies.Add(movie); await _db.SaveChangesAsync(); }

    public async Task RemoveMovieAsync(Guid playlistId, Guid movieId)
    {
        var pm = await _db.PlaylistMovies.FirstOrDefaultAsync(x => x.PlaylistId == playlistId && x.MovieId == movieId);
        if (pm is not null) { _db.PlaylistMovies.Remove(pm); await _db.SaveChangesAsync(); }
    }
}