using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReviewService.DTOs;
using ReviewService.Services;

namespace ReviewService.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _service;
    public ReviewsController(IReviewService service) => _service = service;

    /// <summary>Отзывы к конкретному фильму</summary>
    [HttpGet("movie/{movieId:guid}")]
    public async Task<IActionResult> GetByMovie(Guid movieId)
        => Ok(await _service.GetByMovieAsync(movieId));

    /// <summary>Отзывы пользователя</summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId)
        => Ok(await _service.GetByUserAsync(userId));

    /// <summary>Конкретный отзыв</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>Написать отзыв</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateReviewCommand cmd)
    {
        var result = await _service.CreateAsync(cmd, User);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Обновить свой отзыв</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewCommand cmd)
        => Ok(await _service.UpdateAsync(id, cmd, User));

    /// <summary>Удалить отзыв (свой или admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id, User);
        return NoContent();
    }

    [HttpPost("{id:guid}/reaction")]
    [Authorize]
    public async Task<IActionResult> SetReaction(Guid id, [FromBody] ReviewReactionCommand cmd)
        => Ok(await _service.SetReactionAsync(id, cmd, User));
}
