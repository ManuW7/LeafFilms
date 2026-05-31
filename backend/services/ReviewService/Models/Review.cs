namespace ReviewService.Models;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Text { get; set; } = string.Empty;
    public int LikesCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}