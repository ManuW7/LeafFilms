using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReviewService.Services;

namespace ReviewService.Events;

public class MovieDeletedEvent
{
    public Guid MovieId { get; set; }
}

public class MovieDeletedConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<MovieDeletedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public MovieDeletedConsumer(IConfiguration config, IServiceProvider services, ILogger<MovieDeletedConsumer> logger)
    {
        _config = config;
        _services = services;
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
        _channel.ExchangeDeclare("movie.deleted", ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare("review.movie.deleted", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("review.movie.deleted", "movie.deleted", routingKey: string.Empty);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var evt = JsonSerializer.Deserialize<MovieDeletedEvent>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (evt is not null)
                {
                    using var scope = _services.CreateScope();
                    var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
                    await reviewService.DeleteByMovieAsync(evt.MovieId);
                }
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing movie.deleted");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume("review.movie.deleted", autoAck: false, consumer);
        _logger.LogInformation("MovieDeletedConsumer started");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
