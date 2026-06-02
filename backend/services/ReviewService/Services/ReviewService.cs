using ReviewService.DTOs;
using ReviewService.Events;
using ReviewService.Exceptions;
using ReviewService.Models;
using ReviewService.Repositories;
using System.Security.Claims;

namespace ReviewService.Services;

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetByMovieAsync(Guid movieId);
    Task<IEnumerable<ReviewDto>> GetByUserAsync(Guid userId);
    Task<ReviewDto> GetByIdAsync(Guid id);
    Task<ReviewDto> CreateAsync(CreateReviewCommand cmd, ClaimsPrincipal user);
    Task<ReviewDto> UpdateAsync(Guid id, UpdateReviewCommand cmd, ClaimsPrincipal user);
    Task DeleteAsync(Guid id, ClaimsPrincipal user);
    Task DeleteByMovieAsync(Guid movieId);
}

public class ReviewAppService : IReviewService
{
    private readonly IReviewRepository _repo;
    private readonly IEventPublisher _publisher;
    private readonly ICatalogueClient _catalogue;
    private readonly ILogger<ReviewAppService> _logger;

    public ReviewAppService(
        IReviewRepository repo,
        IEventPublisher publisher,
        ICatalogueClient catalogue,
        ILogger<ReviewAppService> logger)
    {
        _repo = repo;
        _publisher = publisher;
        _catalogue = catalogue;
        _logger = logger;
    }

    public async Task<IEnumerable<ReviewDto>> GetByMovieAsync(Guid movieId)
        => (await _repo.GetByMovieIdAsync(movieId)).Select(ToDto);

    public async Task<IEnumerable<ReviewDto>> GetByUserAsync(Guid userId)
        => (await _repo.GetByUserIdAsync(userId)).Select(ToDto);

    public async Task<ReviewDto> GetByIdAsync(Guid id)
        => ToDto(await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Review", id));

    public async Task<ReviewDto> CreateAsync(CreateReviewCommand cmd, ClaimsPrincipal principal)
    {
        var userId = GetUserId(principal);
        var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        var movie = await _catalogue.GetMovieAsync(cmd.MovieId)
            ?? throw new NotFoundException("Movie", cmd.MovieId);

        if (await _repo.GetByUserAndMovieAsync(userId, cmd.MovieId) is not null)
            throw new ConflictException("You have already reviewed this movie.");

        var review = new Review
        {
            UserId = userId,
            Username = username,
            MovieId = cmd.MovieId,
            MovieTitle = movie.Title,
            Rating = cmd.Rating,
            Text = cmd.Text,
            ImageUrl = string.IsNullOrWhiteSpace(cmd.ImageUrl) ? null : cmd.ImageUrl.Trim()
        };

        var created = await _repo.CreateAsync(review);
        _logger.LogInformation("Review created: {ReviewId} by {UserId}", created.Id, userId);

        await _publisher.PublishAsync("review.created", new ReviewCreatedEvent
        {
            ReviewId = created.Id,
            UserId = created.UserId,
            Username = created.Username,
            MovieId = created.MovieId,
            MovieTitle = created.MovieTitle,
            Rating = created.Rating,
            Text = created.Text,
            ImageUrl = created.ImageUrl,
            CreatedAt = created.CreatedAt
        });

        return ToDto(created);
    }

    public async Task<ReviewDto> UpdateAsync(Guid id, UpdateReviewCommand cmd, ClaimsPrincipal principal)
    {
        var review = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Review", id);

        var userId = GetUserId(principal);
        if (review.UserId != userId)
            throw new ForbiddenException("You can only edit your own reviews.");

        var oldRating = review.Rating;

        if (cmd.Rating.HasValue) review.Rating = cmd.Rating.Value;
        if (cmd.Text is not null) review.Text = cmd.Text;
        if (cmd.ImageUrl is not null) review.ImageUrl = string.IsNullOrWhiteSpace(cmd.ImageUrl) ? null : cmd.ImageUrl.Trim();
        review.UpdatedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(review);
        if (cmd.Rating.HasValue && oldRating != cmd.Rating.Value)
        {
            await _publisher.PublishAsync("review.updated", new ReviewUpdatedEvent
            {
                MovieId = review.MovieId,
                OldRating = oldRating,
                NewRating = cmd.Rating.Value
            });

            _logger.LogInformation(
                "Review {ReviewId} rating changed from {OldRating} to {NewRating}",
                id, oldRating, cmd.Rating.Value);
        }

        return ToDto(updated);
    }

    public async Task DeleteAsync(Guid id, ClaimsPrincipal principal)
    {
        var review = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Review", id);

        var userId = GetUserId(principal);
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;
        if (review.UserId != userId && role != "admin")
            throw new ForbiddenException("You can only delete your own reviews.");

        await _repo.DeleteAsync(id);

        await _publisher.PublishAsync("review.deleted", new ReviewDeletedEvent
        {
            ReviewId = review.Id,
            UserId = review.UserId,
            MovieId = review.MovieId,
            Rating = review.Rating
        });

        _logger.LogInformation("Review {ReviewId} deleted, rating {Rating} removed", id, review.Rating);
    }

    public async Task DeleteByMovieAsync(Guid movieId)
    {
        var reviews = (await _repo.GetByMovieIdAsync(movieId)).ToList();
        if (reviews.Count == 0) return;

        await _repo.DeleteManyAsync(reviews);

        foreach (var review in reviews)
        {
            await _publisher.PublishAsync("review.deleted", new ReviewDeletedEvent
            {
                ReviewId = review.Id,
                UserId = review.UserId,
                MovieId = review.MovieId,
                Rating = review.Rating
            });
        }

        _logger.LogInformation("Deleted {Count} reviews for movie {MovieId}", reviews.Count, movieId);
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirst("sub")
                 ?? throw new UnauthorizedException();
        return Guid.Parse(claim.Value);
    }

    private static ReviewDto ToDto(Review r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Username = r.Username,
        MovieId = r.MovieId,
        MovieTitle = r.MovieTitle,
        Rating = r.Rating,
        Text = r.Text,
        ImageUrl = r.ImageUrl,
        LikesCount = r.LikesCount,
        CreatedAt = r.CreatedAt
    };
}

