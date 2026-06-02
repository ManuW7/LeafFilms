using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FeedService.WebSockets;

public class FeedWebSocketManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<WebSocket>> _connections = new();
    private readonly ILogger<FeedWebSocketManager> _logger;

    public FeedWebSocketManager(ILogger<FeedWebSocketManager> logger) => _logger = logger;

    public void Register(Guid userId, WebSocket ws)
    {
        var bag = _connections.GetOrAdd(userId, _ => new ConcurrentBag<WebSocket>());
        bag.Add(ws);
        _logger.LogInformation("WebSocket registered for user {UserId}. Total connections: {Count}", userId, bag.Count);
    }

    public async Task SendToUserAsync(Guid userId, object payload)
    {
        if (!_connections.TryGetValue(userId, out var bag)) return;

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var dead = new List<WebSocket>();

        foreach (var ws in bag)
        {
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send WS message to user {UserId}", userId);
                    dead.Add(ws);
                }
            }
            else
            {
                dead.Add(ws);
            }
        }
        foreach (var ws in dead)
        {
            var remaining = bag.Where(x => x != ws).ToList();
            _connections[userId] = new ConcurrentBag<WebSocket>(remaining);
        }
    }

    public async Task BroadcastToUsersAsync(IEnumerable<Guid> userIds, object payload)
    {
        var tasks = userIds.Select(id => SendToUserAsync(id, payload));
        await Task.WhenAll(tasks);
    }
    public async Task ListenAsync(Guid userId, WebSocket ws)
    {
        var buffer = new byte[1024];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "WebSocket closed for user {UserId}", userId);
        }
        finally
        {
            _logger.LogInformation("WebSocket disconnected for user {UserId}", userId);
        }
    }
}
