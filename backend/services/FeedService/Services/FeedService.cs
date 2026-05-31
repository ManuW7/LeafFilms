using System.Text.Json;
using FeedService.Models;
using StackExchange.Redis;

namespace FeedService.Services;

public interface IFeedService
{
    Task<IEnumerable<FeedItem>> GetFeedAsync(Guid userId, int page = 1, int pageSize = 20);
    Task PushToFeedAsync(Guid userId, FeedItem item);
    Task PushToManyAsync(IEnumerable<Guid> userIds, FeedItem item);
}

public class RedisFeedService : IFeedService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisFeedService> _logger;
    private const int MaxFeedLength = 200;  // храним максимум 200 событий на пользователя

    public RedisFeedService(IConnectionMultiplexer redis, ILogger<RedisFeedService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IEnumerable<FeedItem>> GetFeedAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var db = _redis.GetDatabase();
        var key = $"feed:{userId}";

        var start = (page - 1) * pageSize;
        var stop = start + pageSize - 1;

        var values = await db.ListRangeAsync(key, start, stop);
        return values
            .Where(v => v.HasValue)
            .Select(v => JsonSerializer.Deserialize<FeedItem>(v!)!)
            .ToList();
    }

    public async Task PushToFeedAsync(Guid userId, FeedItem item)
    {
        var db = _redis.GetDatabase();
        var key = $"feed:{userId}";
        var json = JsonSerializer.Serialize(item);

        // Добавляем в начало списка (новые сверху)
        await db.ListLeftPushAsync(key, json);
        // Обрезаем до максимума
        await db.ListTrimAsync(key, 0, MaxFeedLength - 1);
        // TTL 30 дней
        await db.KeyExpireAsync(key, TimeSpan.FromDays(30));

        _logger.LogDebug("Pushed feed item to user {UserId}", userId);
    }

    public async Task PushToManyAsync(IEnumerable<Guid> userIds, FeedItem item)
    {
        var tasks = userIds.Select(id => PushToFeedAsync(id, item));
        await Task.WhenAll(tasks);
    }
}