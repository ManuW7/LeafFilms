namespace FeedService.Models;

public class FeedItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? ExtraText { get; set; }
    public int? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}