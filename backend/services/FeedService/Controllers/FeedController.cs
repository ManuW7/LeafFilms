using FeedService.Services;
using FeedService.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;

namespace FeedService.Controllers;

[ApiController]
[Route("feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;
    private readonly FeedWebSocketManager _wsManager;
    private readonly ILogger<FeedController> _logger;

    public FeedController(IFeedService feedService, FeedWebSocketManager wsManager, ILogger<FeedController> logger)
    {
        _feedService = feedService;
        _wsManager = wsManager;
        _logger = logger;
    }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);
    [HttpGet]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var items = await _feedService.GetFeedAsync(CurrentUserId, page, pageSize);
        return Ok(items);
    }
    [HttpGet("/ws/feed")]
    [AllowAnonymous]
    public async Task ConnectWebSocket([FromQuery] string? token)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("WebSocket connection expected.");
            return;
        }
        Guid userId;
        if (!TryGetUserIdFromToken(token, out userId))
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("Invalid or missing token.");
            return;
        }

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _logger.LogInformation("WebSocket connection accepted for user {UserId}", userId);

        _wsManager.Register(userId, ws);
        await _wsManager.ListenAsync(userId, ws);
    }

    private bool TryGetUserIdFromToken(string? token, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
            return sub is not null && Guid.TryParse(sub, out userId);
        }
        catch
        {
            return false;
        }
    }
}
