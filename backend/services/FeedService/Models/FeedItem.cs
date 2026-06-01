namespace FeedService.Models;

public class FeedItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public Guid? ReviewId { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? ExtraText { get; set; }
    public string? ImageUrl { get; set; }
    public int? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
