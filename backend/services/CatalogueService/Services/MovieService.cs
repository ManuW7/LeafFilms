using CatalogueService.DTOs;
using CatalogueService.Events;
using CatalogueService.Exceptions;
using CatalogueService.Models;
using CatalogueService.Repositories;
using StackExchange.Redis;
using System.Text.Json;

namespace CatalogueService.Services;

public interface IMovieService
{
    Task<IEnumerable<MovieDto>> GetAllAsync();
    Task<MovieDto> GetByIdAsync(Guid id);
    Task<IEnumerable<MovieDto>> SearchAsync(string query);
    Task<MovieDto> CreateAsync(CreateMovieCommand cmd);
    Task<MovieDto> UpdateAsync(Guid id, UpdateMovieCommand cmd);
    Task DeleteAsync(Guid id);
}

public class MovieService : IMovieService
{
    private readonly IMovieRepository _repo;
    private readonly IConnectionMultiplexer _redis;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<MovieService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public MovieService(IMovieRepository repo, IConnectionMultiplexer redis, IEventPublisher publisher, ILogger<MovieService> logger)
    {
        _repo = repo;
        _redis = redis;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<IEnumerable<MovieDto>> GetAllAsync()
    {
        var db = _redis.GetDatabase();
        const string key = "movies:all";

        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<IEnumerable<MovieDto>>((string)cached!)!;
        }

        var movies = await _repo.GetAllAsync();
        var dtos = movies.Select(ToDto).ToList();
        await db.StringSetAsync(key, JsonSerializer.Serialize(dtos), CacheTtl);
        return dtos;
    }

    public async Task<MovieDto> GetByIdAsync(Guid id)
    {
        var db = _redis.GetDatabase();
        var key = $"movie:{id}";

        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<MovieDto>((string)cached!)!;
        }

        var movie = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Movie", id);

        var dto = ToDto(movie);
        await db.StringSetAsync(key, JsonSerializer.Serialize(dto), CacheTtl);
        return dto;
    }

    public async Task<IEnumerable<MovieDto>> SearchAsync(string query)
    {
        var movies = await _repo.SearchAsync(query);
        return movies.Select(ToDto);
    }

    public async Task<MovieDto> CreateAsync(CreateMovieCommand cmd)
    {
        var movie = new Movie
        {
            Title = cmd.Title,
            TitleRu = cmd.TitleRu,
            Year = cmd.Year,
            Genre = cmd.Genre,
            Director = cmd.Director,
            Description = cmd.Description,
            PosterUrl = cmd.PosterUrl
        };

        var created = await _repo.CreateAsync(movie);
        await InvalidateCacheAsync();
        return ToDto(created);
    }

    public async Task<MovieDto> UpdateAsync(Guid id, UpdateMovieCommand cmd)
    {
        var movie = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Movie", id);

        if (cmd.Title is not null) movie.Title = cmd.Title;
        if (cmd.TitleRu is not null) movie.TitleRu = string.IsNullOrWhiteSpace(cmd.TitleRu) ? null : cmd.TitleRu;
        if (cmd.Year.HasValue) movie.Year = cmd.Year.Value;
        if (cmd.Genre is not null) movie.Genre = cmd.Genre;
        if (cmd.Director is not null) movie.Director = cmd.Director;
        if (cmd.Description is not null) movie.Description = cmd.Description;
        if (cmd.PosterUrl is not null) movie.PosterUrl = cmd.PosterUrl;

        var updated = await _repo.UpdateAsync(movie);
        await InvalidateCacheAsync(id);
        return ToDto(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        await InvalidateCacheAsync(id);
        await _publisher.PublishAsync("movie.deleted", new MovieDeletedEvent { MovieId = id });
    }

    private async Task InvalidateCacheAsync(Guid? id = null)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync("movies:all");
        if (id.HasValue)
            await db.KeyDeleteAsync($"movie:{id}");
    }

    private static MovieDto ToDto(Movie m) => new()
    {
        Id = m.Id,
        Title = m.Title,
        TitleRu = m.TitleRu,
        Year = m.Year,
        Genre = m.Genre,
        Director = m.Director,
        Description = m.Description,
        PosterUrl = m.PosterUrl,
        AverageRating = m.AverageRating,
        ReviewCount = m.ReviewCount
    };
}
