using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialService.DTOs;
using SocialService.Exceptions;
using SocialService.Models;
using SocialService.Repositories;
using System.Security.Claims;

namespace SocialService.Controllers;

[ApiController]
[Authorize]
public class FollowController : ControllerBase
{
    private readonly IFollowRepository _repo;
    private readonly ILogger<FollowController> _logger;

    public FollowController(IFollowRepository repo, ILogger<FollowController> logger)
    { _repo = repo; _logger = logger; }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    private string CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

    /// <summary>Подписаться на пользователя</summary>
    [HttpPost("follow")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Follow([FromBody] FollowCommand cmd)
    {
        var followerId = CurrentUserId;

        if (followerId == cmd.FollowedId)
            throw new BadRequestException("You cannot follow yourself.");

        if (await _repo.GetAsync(followerId, cmd.FollowedId) is not null)
            throw new ConflictException("You are already following this user.");

        var follow = new Follow
        {
            FollowerId = followerId,
            FollowerName = CurrentUsername,
            FollowedId = cmd.FollowedId
        };

        var created = await _repo.CreateAsync(follow);
        _logger.LogInformation("User {FollowerId} followed {FollowedId}", followerId, cmd.FollowedId);
        return StatusCode(201, new FollowDto
        {
            Id = created.Id,
            FollowerId = created.FollowerId,
            FollowerName = created.FollowerName,
            FollowedId = created.FollowedId,
            CreatedAt = created.CreatedAt
        });
    }

    /// <summary>Отписаться от пользователя</summary>
    [HttpDelete("follow")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unfollow([FromBody] FollowCommand cmd)
    {
        var followerId = CurrentUserId;
        await _repo.DeleteAsync(followerId, cmd.FollowedId);
        return NoContent();
    }

    /// <summary>Подписчики пользователя</summary>
    [HttpGet("users/{userId:guid}/followers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowers(Guid userId)
    {
        var followers = await _repo.GetFollowersAsync(userId);
        return Ok(followers.Select(f => new UserSummaryDto
        {
            UserId = f.FollowerId,
            Username = f.FollowerName,
            FollowedAt = f.CreatedAt
        }));
    }

    [HttpGet("users/{userId:guid}/following")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowing(Guid userId)
    {
        var following = await _repo.GetFollowingAsync(userId);
        return Ok(following.Select(f => new UserSummaryDto
        {
            UserId = f.FollowedId,
            Username = f.FollowedName,
            FollowedAt = f.CreatedAt
        }));
    }
}