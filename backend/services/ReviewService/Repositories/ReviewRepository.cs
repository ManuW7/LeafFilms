using Microsoft.EntityFrameworkCore;
using ReviewService.Data;
using ReviewService.Models;

namespace ReviewService.Repositories;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetByMovieIdAsync(Guid movieId);
    Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId);
    Task<Review?> GetByIdAsync(Guid id);
    Task<Review?> GetByUserAndMovieAsync(Guid userId, Guid movieId);
    Task<Review> CreateAsync(Review review);
    Task<Review> UpdateAsync(Review review);
    Task DeleteAsync(Guid id);
    Task DeleteManyAsync(IEnumerable<Review> reviews);
}

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Review>> GetByMovieIdAsync(Guid movieId)
        => await _db.Reviews.AsNoTracking()
               .Where(r => r.MovieId == movieId)
               .OrderByDescending(r => r.CreatedAt)
               .ToListAsync();

    public async Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId)
        => await _db.Reviews.AsNoTracking()
               .Where(r => r.UserId == userId)
               .OrderByDescending(r => r.CreatedAt)
               .ToListAsync();

    public async Task<Review?> GetByIdAsync(Guid id)
        => await _db.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Review?> GetByUserAndMovieAsync(Guid userId, Guid movieId)
        => await _db.Reviews.AsNoTracking()
               .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);

    public async Task<Review> CreateAsync(Review review)
    { _db.Reviews.Add(review); await _db.SaveChangesAsync(); return review; }

    public async Task<Review> UpdateAsync(Review review)
    { _db.Reviews.Update(review); await _db.SaveChangesAsync(); return review; }

    public async Task DeleteAsync(Guid id)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r is not null) { _db.Reviews.Remove(r); await _db.SaveChangesAsync(); }
    }

    public async Task DeleteManyAsync(IEnumerable<Review> reviews)
    {
        _db.Reviews.RemoveRange(reviews);
        await _db.SaveChangesAsync();
    }
}
