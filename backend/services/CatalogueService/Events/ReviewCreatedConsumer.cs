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

public class ReviewCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<ReviewCreatedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "review.created";
    private const string QueueName = "catalogue.review.created";

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

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, routingKey: string.Empty);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += HandleMessageAsync;

            _channel.BasicConsume(QueueName, autoAck: false, consumer);
            _logger.LogInformation("ReviewCreatedConsumer started, listening to {Queue}", QueueName);
        }
        catch (Exception ex)
        {
            // RabbitMQ может быть недоступен при локальной разработке — не падаем
            _logger.LogWarning(ex, "Could not connect to RabbitMQ. Rating updates will be unavailable.");
        }

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            var evt = JsonSerializer.Deserialize<ReviewCreatedEvent>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt is null)
            {
                _channel?.BasicAck(ea.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "Received ReviewCreated for movie {MovieId}, rating {Rating}",
                evt.MovieId, evt.Rating);

            await UpdateMovieRatingAsync(evt.MovieId, evt.Rating);

            _channel?.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReviewCreated event: {Body}", body);
            _channel?.BasicNack(ea.DeliveryTag, false, requeue: true);
        }
    }

    private async Task UpdateMovieRatingAsync(Guid movieId, int newRating)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == movieId);
        if (movie is null)
        {
            _logger.LogWarning("Movie {MovieId} not found, skipping rating update", movieId);
            return;
        }

        // Пересчёт среднего рейтинга: (старый_avg * кол-во + новый) / (кол-во + 1)
        movie.AverageRating = Math.Round(
            (movie.AverageRating * movie.ReviewCount + newRating) / (movie.ReviewCount + 1.0),
            2);
        movie.ReviewCount += 1;

        await db.SaveChangesAsync();

        // Инвалидируем кэш Redis
        var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var redisDb = redis.GetDatabase();
        await redisDb.KeyDeleteAsync("movies:all");
        await redisDb.KeyDeleteAsync($"movie:{movieId}");

        _logger.LogInformation(
            "Movie {MovieId} rating updated: {Rating} ({Count} reviews)",
            movieId, movie.AverageRating, movie.ReviewCount);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}