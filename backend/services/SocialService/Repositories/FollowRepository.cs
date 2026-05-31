using Microsoft.EntityFrameworkCore;
using SocialService.Data;
using SocialService.Models;

namespace SocialService.Repositories;

public interface IFollowRepository
{
    Task<Follow?> GetAsync(Guid followerId, Guid followedId);
    Task<IEnumerable<Follow>> GetFollowersAsync(Guid userId);
    Task<IEnumerable<Follow>> GetFollowingAsync(Guid userId);
    Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userId);
    Task<Follow> CreateAsync(Follow follow);
    Task DeleteAsync(Guid followerId, Guid followedId);
}

public class FollowRepository : IFollowRepository
{
    private readonly AppDbContext _db;
    public FollowRepository(AppDbContext db) => _db = db;

    public async Task<Follow?> GetAsync(Guid followerId, Guid followedId)
        => await _db.Follows.AsNoTracking()
               .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

    public async Task<IEnumerable<Follow>> GetFollowersAsync(Guid userId)
        => await _db.Follows.AsNoTracking().Where(f => f.FollowedId == userId).ToListAsync();

    public async Task<IEnumerable<Follow>> GetFollowingAsync(Guid userId)
        => await _db.Follows.AsNoTracking().Where(f => f.FollowerId == userId).ToListAsync();

    public async Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userId)
        => await _db.Follows.AsNoTracking()
               .Where(f => f.FollowedId == userId)
               .Select(f => f.FollowerId)
               .ToListAsync();

    public async Task<Follow> CreateAsync(Follow follow)
    { _db.Follows.Add(follow); await _db.SaveChangesAsync(); return follow; }

    public async Task DeleteAsync(Guid followerId, Guid followedId)
    {
        var f = await _db.Follows.FirstOrDefaultAsync(x => x.FollowerId == followerId && x.FollowedId == followedId);
        if (f is not null) { _db.Follows.Remove(f); await _db.SaveChangesAsync(); }
    }
}