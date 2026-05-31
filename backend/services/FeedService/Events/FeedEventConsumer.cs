using System.Text;
using System.Text.Json;
using FeedService.Models;
using FeedService.Services;
using FeedService.WebSockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FeedService.Events;

// ── Incoming event shapes ─────────────────────────────────────────────────────

public class ReviewCreatedEvent
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class MovieWatchedEvent
{
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime WatchedAt { get; set; }
}

// ── Consumer background service ───────────────────────────────────────────────

public class FeedEventConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;
    private readonly FeedWebSocketManager _wsManager;
    private readonly ILogger<FeedEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public FeedEventConsumer(
        IConfiguration config,
        IServiceProvider services,
        FeedWebSocketManager wsManager,
        ILogger<FeedEventConsumer> logger)
    {
        _config = config;
        _services = services;
        _wsManager = wsManager;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "localhost",
            UserName = _config["RabbitMQ:User"] ?? "guest",
            Password = _config["RabbitMQ:Pass"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        SubscribeToExchange("review.created", "feed.review.created", HandleReviewCreatedAsync);
        SubscribeToExchange("movie.watched", "feed.movie.watched", HandleMovieWatchedAsync);

        _logger.LogInformation("FeedEventConsumer started, listening to RabbitMQ");
        return Task.CompletedTask;
    }

    private void SubscribeToExchange(string exchange, string queue, Func<string, Task> handler)
    {
        _channel!.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue, exchange, routingKey: string.Empty);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await handler(body);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Exchange}", exchange);
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue, autoAck: false, consumer);
    }

    private async Task HandleReviewCreatedAsync(string json)
    {
        var evt = JsonSerializer.Deserialize<ReviewCreatedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt is null) return;

        _logger.LogInformation("Received event ReviewCreated for movie {MovieTitle} by {Username}",
            evt.MovieTitle, evt.Username);

        using var scope = _services.CreateScope();
        var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();
        var socialClient = scope.ServiceProvider.GetRequiredService<ISocialClient>();

        var followerIds = (await socialClient.GetFollowerIdsAsync(evt.UserId)).ToList();
        _logger.LogInformation("Updated feed for {Count} followers", followerIds.Count);

        var feedItem = new FeedItem
        {
            EventType = "review_created",
            ActorId = evt.UserId,
            ActorName = evt.Username,
            MovieId = evt.MovieId,
            MovieTitle = evt.MovieTitle,
            ExtraText = evt.Text,
            Rating = evt.Rating,
            CreatedAt = evt.CreatedAt
        };

        await feedService.PushToManyAsync(followerIds, feedItem);

        // Уведомляем по WebSocket
        await _wsManager.BroadcastToUsersAsync(followerIds, new
        {
            type = "review_created",
            actorName = evt.Username,
            movieTitle = evt.MovieTitle,
            rating = evt.Rating,
            createdAt = evt.CreatedAt
        });
    }

    private async Task HandleMovieWatchedAsync(string json)
    {
        var evt = JsonSerializer.Deserialize<MovieWatchedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt is null) return;

        _logger.LogInformation("Received event MovieWatched: {MovieTitle} by {UserId}",
            evt.MovieTitle, evt.UserId);

        using var scope = _services.CreateScope();
        var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();
        var socialClient = scope.ServiceProvider.GetRequiredService<ISocialClient>();

        var followerIds = (await socialClient.GetFollowerIdsAsync(evt.UserId)).ToList();

        var feedItem = new FeedItem
        {
            EventType = "movie_watched",
            ActorId = evt.UserId,
            MovieId = evt.MovieId,
            MovieTitle = evt.MovieTitle,
            CreatedAt = evt.WatchedAt
        };

        await feedService.PushToManyAsync(followerIds, feedItem);

        await _wsManager.BroadcastToUsersAsync(followerIds, new
        {
            type = "movie_watched",
            movieTitle = evt.MovieTitle,
            watchedAt = evt.WatchedAt
        });
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}