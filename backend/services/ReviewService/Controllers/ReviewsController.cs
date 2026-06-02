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
    [HttpGet("movie/{movieId:guid}")]
    public async Task<IActionResult> GetByMovie(Guid movieId)
        => Ok(await _service.GetByMovieAsync(movieId));
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId)
        => Ok(await _service.GetByUserAsync(userId));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));
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
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewCommand cmd)
        => Ok(await _service.UpdateAsync(id, cmd, User));
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id, User);
        return NoContent();
    }
}
