using System.ComponentModel.DataAnnotations;

namespace ReviewService.DTOs;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewCommand
{
    [Required]
    public Guid MovieId { get; set; }

    [Required, Range(1, 10)]
    public int Rating { get; set; }

    [Required, MinLength(10), MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    [Url, MaxLength(1000)]
    public string? ImageUrl { get; set; }
}

public class UpdateReviewCommand
{
    [Range(1, 10)] public int? Rating { get; set; }
    [MinLength(10), MaxLength(2000)] public string? Text { get; set; }
    [Url, MaxLength(1000)] public string? ImageUrl { get; set; }
}

public class ReviewReactionCommand
{
    [Range(-1, 1)]
    public int Value { get; set; }
}

public class ReviewReactionDto
{
    public Guid ReviewId { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public int MyReaction { get; set; }
}
