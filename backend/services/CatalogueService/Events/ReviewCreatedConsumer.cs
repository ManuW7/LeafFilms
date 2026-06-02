using System.Text;
using System.Text.Json;
using CatalogueService.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CatalogueService.Events;

public class ReviewCreatedEvent
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReviewUpdatedEvent
{
    public Guid MovieId { get; set; }
    public int OldRating { get; set; }
    public int NewRating { get; set; }
}

public class ReviewDeletedEvent
{
    public Guid MovieId { get; set; }
    public int Rating { get; set; }
}

public class ReviewCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<ReviewCreatedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ReviewCreatedConsumer(
        IConfiguration config,
        IServiceProvider services,
        ILogger<ReviewCreatedConsumer> logger)
    {
        _config = config;
        _services = services;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
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

            Subscribe("review.created", "catalogue.review.created", HandleReviewCreatedAsync);
            Subscribe("review.updated", "catalogue.review.updated", HandleReviewUpdatedAsync);
            Subscribe("review.deleted", "catalogue.review.deleted", HandleReviewDeletedAsync);

            _logger.LogInformation("ReviewConsumer started");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not connect to RabbitMQ. Rating updates will be unavailable.");
        }

        return Task.CompletedTask;
    }

    private void Subscribe(string exchange, string queue, Func<string, Task> handler)
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
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Exchange}", exchange);
                _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
            }
        };

        _channel.BasicConsume(queue, autoAck: false, consumer);
    }

    private async Task HandleReviewCreatedAsync(string json)
    {
        var evt = JsonSerializer.Deserialize<ReviewCreatedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt is null) return;

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == evt.MovieId);
        if (movie is null) return;

        movie.AverageRating = Math.Round(
            (movie.AverageRating * movie.ReviewCount + evt.Rating) / (movie.ReviewCount + 1.0), 2);
        movie.ReviewCount += 1;

        await db.SaveChangesAsync();
        await InvalidateCacheAsync(scope, evt.MovieId);

        _logger.LogInformation("Movie {MovieId} rating updated to {Rating} ({Count} reviews)",
            evt.MovieId, movie.AverageRating, movie.ReviewCount);
    }

    private async Task HandleReviewUpdatedAsync(string json)
    {
        var evt = JsonSerializer.Deserialize<ReviewUpdatedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt is null) return;

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == evt.MovieId);
        if (movie is null || movie.ReviewCount == 0) return;
        movie.AverageRating = Math.Round(
            (movie.AverageRating * movie.ReviewCount - evt.OldRating + evt.NewRating) / (double)movie.ReviewCount, 2);

        await db.SaveChangesAsync();
        await InvalidateCacheAsync(scope, evt.MovieId);

        _logger.LogInformation("Movie {MovieId} rating recalculated to {Rating} after review update",
            evt.MovieId, movie.AverageRating);
    }

    private async Task HandleReviewDeletedAsync(string json)
    {
        var evt = JsonSerializer.Deserialize<ReviewDeletedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt is null) return;

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == evt.MovieId);
        if (movie is null || movie.ReviewCount == 0) return;

        if (movie.ReviewCount == 1)
        {
            movie.AverageRating = 0;
            movie.ReviewCount = 0;
        }
        else
        {
            movie.AverageRating = Math.Round(
                (movie.AverageRating * movie.ReviewCount - evt.Rating) / (movie.ReviewCount - 1.0), 2);
            movie.ReviewCount -= 1;
        }

        await db.SaveChangesAsync();
        await InvalidateCacheAsync(scope, evt.MovieId);

        _logger.LogInformation("Movie {MovieId} rating recalculated to {Rating} after review deletion",
            evt.MovieId, movie.AverageRating);
    }

    private static async Task InvalidateCacheAsync(IServiceScope scope, Guid movieId)
    {
        var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync("movies:all");
        await db.KeyDeleteAsync($"movie:{movieId}");
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

