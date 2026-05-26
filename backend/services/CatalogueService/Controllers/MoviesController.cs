using CatalogueService.DTOs;
using CatalogueService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogueService.Controllers;

[ApiController]
[Route("movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService) => _movieService = movieService;

    /// <summary>Список всех фильмов (с кэшированием Redis)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _movieService.GetAllAsync());

    /// <summary>Поиск фильмов по названию, режиссёру, жанру</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");
        return Ok(await _movieService.SearchAsync(q));
    }

    /// <summary>Получить фильм по ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _movieService.GetByIdAsync(id));

    /// <summary>Добавить фильм (только admin)</summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateMovieCommand cmd)
    {
        var result = await _movieService.CreateAsync(cmd);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Обновить фильм (только admin)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMovieCommand cmd)
        => Ok(await _movieService.UpdateAsync(id, cmd));

    /// <summary>Удалить фильм (только admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _movieService.DeleteAsync(id);
        return NoContent();
    }
}