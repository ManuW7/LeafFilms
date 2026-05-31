using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ActivityService.Events;

public class MovieWatchedEvent
{
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime WatchedAt { get; set; }
}

public interface IEventPublisher
{
    Task PublishAsync<T>(string exchangeName, T @event);
}

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IConfiguration config, ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Pass"] ?? "guest"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task PublishAsync<T>(string exchangeName, T @event)
    {
        _channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, durable: true);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        _channel.BasicPublish(exchange: exchangeName, routingKey: string.Empty, basicProperties: props, body: body);
        _logger.LogInformation("Published {EventType} to {Exchange}", typeof(T).Name, exchangeName);
        return Task.CompletedTask;
    }

    public void Dispose() { _channel?.Close(); _connection?.Close(); }
}