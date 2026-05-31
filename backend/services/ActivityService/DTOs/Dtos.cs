using System.ComponentModel.DataAnnotations;

namespace ActivityService.DTOs;

public class AddToHistoryCommand
{
    [Required] public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
}

public class WatchHistoryDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime WatchedAt { get; set; }
}

public class AddToWatchlistCommand
{
    [Required] public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
}

public class WatchlistDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

public class CreatePlaylistCommand
{
    [Required, MinLength(1), MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class PlaylistDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<PlaylistMovieDto> Movies { get; set; } = new();
}

public class PlaylistMovieDto
{
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

public class AddToPlaylistCommand
{
    [Required] public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
}