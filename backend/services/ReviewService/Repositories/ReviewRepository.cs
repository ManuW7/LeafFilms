using Microsoft.EntityFrameworkCore;
using ReviewService.Data;
using ReviewService.Exceptions;
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
    Task<ReviewReaction?> GetReactionAsync(Guid reviewId, Guid userId);
    Task<ReviewReactionDtoState> SetReactionAsync(Guid reviewId, Guid userId, int value);
}

public record ReviewReactionDtoState(int LikesCount, int DislikesCount, int MyReaction);

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

    public async Task<ReviewReaction?> GetReactionAsync(Guid reviewId, Guid userId)
        => await _db.ReviewReactions.AsNoTracking()
               .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

    public async Task<ReviewReactionDtoState> SetReactionAsync(Guid reviewId, Guid userId, int value)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new NotFoundException("Review", reviewId);

        var existing = await _db.ReviewReactions
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

        if (existing is not null)
        {
            if (existing.Value == 1) review.LikesCount = Math.Max(0, review.LikesCount - 1);
            if (existing.Value == -1) review.DislikesCount = Math.Max(0, review.DislikesCount - 1);

            if (value == 0)
            {
                _db.ReviewReactions.Remove(existing);
            }
            else
            {
                existing.Value = value;
            }
        }
        else if (value != 0)
        {
            _db.ReviewReactions.Add(new ReviewReaction { ReviewId = reviewId, UserId = userId, Value = value });
        }

        if (value == 1) review.LikesCount += 1;
        if (value == -1) review.DislikesCount += 1;

        await _db.SaveChangesAsync();
        return new ReviewReactionDtoState(review.LikesCount, review.DislikesCount, value);
    }
}
