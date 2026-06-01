using System.ComponentModel.DataAnnotations;

namespace CatalogueService.DTOs;

public class MovieDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleRu { get; set; }
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Director { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class CreateMovieCommand
{
    [Required, MinLength(1), MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? TitleRu { get; set; }

    [Required, Range(1888, 2100)]
    public int Year { get; set; }

    [Required, MaxLength(100)]
    public string Genre { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Director { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
}

public class UpdateMovieCommand
{
    [MaxLength(300)] public string? Title { get; set; }
    [MaxLength(300)] public string? TitleRu { get; set; }
    [Range(1888, 2100)] public int? Year { get; set; }
    [MaxLength(100)] public string? Genre { get; set; }
    [MaxLength(200)] public string? Director { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
}
