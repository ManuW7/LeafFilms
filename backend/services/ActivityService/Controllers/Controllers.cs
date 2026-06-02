using ActivityService.DTOs;
using ActivityService.Events;
using ActivityService.Exceptions;
using ActivityService.Models;
using ActivityService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActivityService.Controllers;

[ApiController]
[Route("history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IWatchHistoryRepository _repo;
    private readonly IEventPublisher _publisher;

    public HistoryController(IWatchHistoryRepository repo, IEventPublisher publisher)
    { _repo = repo; _publisher = publisher; }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var items = await _repo.GetByUserAsync(CurrentUserId);
        return Ok(items.Select(x => new WatchHistoryDto
        {
            Id = x.Id,
            MovieId = x.MovieId,
            MovieTitle = x.MovieTitle,
            WatchedAt = x.WatchedAt
        }));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddToHistory([FromBody] AddToHistoryCommand cmd)
    {
        var userId = CurrentUserId;
        var existing = await _repo.GetAsync(userId, cmd.MovieId);
        if (existing is not null)
        {
            return Ok(new WatchHistoryDto
            {
                Id = existing.Id,
                MovieId = existing.MovieId,
                MovieTitle = existing.MovieTitle,
                WatchedAt = existing.WatchedAt
            });
        }

        var entry = new WatchHistory
        {
            UserId = userId,
            MovieId = cmd.MovieId,
            MovieTitle = cmd.MovieTitle
        };
        var created = await _repo.AddAsync(entry);

        await _publisher.PublishAsync("movie.watched", new MovieWatchedEvent
        {
            UserId = userId,
            Username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown",
            MovieId = cmd.MovieId,
            MovieTitle = cmd.MovieTitle,
            WatchedAt = created.WatchedAt
        });

        return StatusCode(201, new WatchHistoryDto
        {
            Id = created.Id,
            MovieId = created.MovieId,
            MovieTitle = created.MovieTitle,
            WatchedAt = created.WatchedAt
        });
    }
}

[ApiController]
[Route("watchlist")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistRepository _repo;

    public WatchlistController(IWatchlistRepository repo) => _repo = repo;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _repo.GetByUserAsync(CurrentUserId);
        return Ok(items.Select(x => new WatchlistDto
        {
            Id = x.Id,
            MovieId = x.MovieId,
            MovieTitle = x.MovieTitle,
            AddedAt = x.AddedAt
        }));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] AddToWatchlistCommand cmd)
    {
        var userId = CurrentUserId;
        if (await _repo.GetAsync(userId, cmd.MovieId) is not null)
            throw new ConflictException("Movie is already in watchlist.");

        var item = new WatchlistItem
        {
            UserId = userId,
            MovieId = cmd.MovieId,
            MovieTitle = cmd.MovieTitle
        };
        var created = await _repo.AddAsync(item);
        return StatusCode(201, new WatchlistDto
        {
            Id = created.Id,
            MovieId = created.MovieId,
            MovieTitle = created.MovieTitle,
            AddedAt = created.AddedAt
        });
    }

    [HttpDelete("{movieId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid movieId)
    {
        await _repo.RemoveAsync(CurrentUserId, movieId);
        return NoContent();
    }
}

[ApiController]
[Route("playlists")]
[Authorize]
public class PlaylistsController : ControllerBase
{
    private readonly IPlaylistRepository _repo;

    public PlaylistsController(IPlaylistRepository repo) => _repo = repo;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var playlists = await _repo.GetByUserAsync(CurrentUserId);
        return Ok(playlists.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Playlist", id);
        if (p.UserId != CurrentUserId) throw new ForbiddenException();
        return Ok(ToDto(p));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePlaylistCommand cmd)
    {
        var playlist = new Playlist { UserId = CurrentUserId, Name = cmd.Name };
        var created = await _repo.CreateAsync(playlist);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Playlist", id);
        if (p.UserId != CurrentUserId) throw new ForbiddenException();
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/movies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMovie(Guid id, [FromBody] AddToPlaylistCommand cmd)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Playlist", id);
        if (p.UserId != CurrentUserId) throw new ForbiddenException();

        await _repo.AddMovieAsync(new PlaylistMovie
        {
            PlaylistId = id,
            MovieId = cmd.MovieId,
            MovieTitle = cmd.MovieTitle
        });
        return StatusCode(201);
    }

    [HttpDelete("{id:guid}/movies/{movieId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMovie(Guid id, Guid movieId)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Playlist", id);
        if (p.UserId != CurrentUserId) throw new ForbiddenException();
        await _repo.RemoveMovieAsync(id, movieId);
        return NoContent();
    }

    private static PlaylistDto ToDto(Playlist p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        CreatedAt = p.CreatedAt,
        Movies = p.Movies.Select(m => new PlaylistMovieDto
        {
            MovieId = m.MovieId,
            MovieTitle = m.MovieTitle,
            AddedAt = m.AddedAt
        }).ToList()
    };
}

