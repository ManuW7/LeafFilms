using System.ComponentModel.DataAnnotations;

namespace SocialService.DTOs;

public class FollowCommand
{
    [Required]
    public Guid FollowedId { get; set; }

    public string? FollowedName { get; set; }
}

public class FollowDto
{
    public Guid Id { get; set; }
    public Guid FollowerId { get; set; }
    public string FollowerName { get; set; } = string.Empty;
    public Guid FollowedId { get; set; }
    public string FollowedName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserSummaryDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
}
