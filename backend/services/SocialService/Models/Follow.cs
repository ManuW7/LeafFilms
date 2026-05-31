namespace SocialService.Models;

public class Follow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FollowerId { get; set; }  
    public string FollowerName { get; set; } = string.Empty;
    public Guid FollowedId { get; set; }   
    public string FollowedName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}