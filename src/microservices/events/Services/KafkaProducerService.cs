using System.Text.Json;
using Confluent.Kafka;
using EventsService.Models;

namespace EventsService.Services;

public interface IKafkaProducerService
{
    Task<EventResponse> ProduceEventAsync(string eventType, object payload, string topic);
    Task<EventResponse> ProduceMovieEventAsync(MovieEventPayload payload);
    Task<EventResponse> ProduceUserEventAsync(UserEventPayload payload);
    Task<EventResponse> ProducePaymentEventAsync(PaymentEventPayload payload);
}

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly Dictionary<string, string> _topics;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        
        _topics = new Dictionary<string, string>
        {
            ["movie"] = configuration["Kafka:Topics:Movie"] ?? "movie-events",
            ["user"] = configuration["Kafka:Topics:User"] ?? "user-events",
            ["payment"] = configuration["Kafka:Topics:Payment"] ?? "payment-events"
        };
        
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = "events-service-producer",
            Acks = Acks.Leader,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
        
        _logger.LogInformation("Kafka producer initialized. BootstrapServers: {BootstrapServers}", bootstrapServers);
        _logger.LogInformation("Topics: Movie={MovieTopic}, User={UserTopic}, Payment={PaymentTopic}", 
            _topics["movie"], _topics["user"], _topics["payment"]);
    }

    public async Task<EventResponse> ProduceEventAsync(string eventType, object payload, string topic)
    {
        try
        {
            var @event = new Event
            {
                Id = Guid.NewGuid().ToString(),
                Type = eventType,
                Timestamp = DateTime.UtcNow,
                Payload = payload
            };

            var message = JsonSerializer.Serialize(@event);
            
            var result = await _producer.ProduceAsync(topic, new Message<Null, string> 
            { 
                Value = message 
            });
            
            _logger.LogInformation("Event produced to Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Type: {Type}",
                result.Topic, result.Partition.Value, result.Offset.Value, eventType);
            
            return new EventResponse
            {
                Status = "success",
                Partition = result.Partition.Value,
                Offset = result.Offset.Value,
                Event = @event
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce event to Kafka. Type: {Type}, Topic: {Topic}", eventType, topic);
            throw;
        }
    }

    public Task<EventResponse> ProduceMovieEventAsync(MovieEventPayload payload)
    {
        return ProduceEventAsync("movie", payload, _topics["movie"]);
    }

    public Task<EventResponse> ProduceUserEventAsync(UserEventPayload payload)
    {
        return ProduceEventAsync("user", payload, _topics["user"]);
    }

    public Task<EventResponse> ProducePaymentEventAsync(PaymentEventPayload payload)
    {
        return ProduceEventAsync("payment", payload, _topics["payment"]);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}
