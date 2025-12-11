using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using EventsService.Models;

namespace EventsService.Services;

public interface IKafkaConsumerService
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
}

public class KafkaConsumerService : IKafkaConsumerService, IDisposable
{
    private readonly List<IConsumer<Ignore, string>> _consumers;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly Dictionary<string, string> _topics;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService> logger)
    {
        _logger = logger;
        _consumers = new List<IConsumer<Ignore, string>>();
        
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var groupId = configuration["Kafka:GroupId"] ?? "events-service";
        
        _topics = new Dictionary<string, string>
        {
            ["movie"] = configuration["Kafka:Topics:Movie"] ?? "movie-events",
            ["user"] = configuration["Kafka:Topics:User"] ?? "user-events",
            ["payment"] = configuration["Kafka:Topics:Payment"] ?? "payment-events"
        };
        
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false
        };

        foreach (var topic in _topics.Values)
        {
            var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(topic);
            _consumers.Add(consumer);
            
            _logger.LogInformation("Kafka consumer subscribed to topic: {Topic}", topic);
        }
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        _logger.LogInformation("Kafka consumers initialized. BootstrapServers: {BootstrapServers}, GroupId: {GroupId}", 
            bootstrapServers, groupId);
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Kafka consumers for {Count} topics...", _consumers.Count);
        
        var consumerTasks = new List<Task>();
        
        foreach (var consumer in _consumers)
        {
            consumerTasks.Add(Task.Run(() => ConsumeFromTopicAsync(consumer, cancellationToken), cancellationToken));
        }
        
        await Task.WhenAll(consumerTasks);
    }

    private async Task ConsumeFromTopicAsync(IConsumer<Ignore, string> consumer, CancellationToken cancellationToken)
    {
        var topic = string.Join(", ", consumer.Subscription);
        
        _logger.LogInformation("Started consuming from topic(s): {Topic}", topic);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);
                
                if (consumeResult?.Message?.Value == null)
                    continue;

                await ProcessMessageAsync(
                    consumeResult.Message.Value, 
                    consumeResult.Topic,
                    consumeResult.Partition.Value, 
                    consumeResult.Offset.Value);
                
                consumer.StoreOffset(consumeResult);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming from Kafka topic: {Topic}", topic);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer stopped for topic: {Topic}", topic);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Kafka consumer for topic: {Topic}", topic);
            }
        }
    }

    private async Task ProcessMessageAsync(string message, string topic, int partition, long offset)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<Event>(message, _jsonOptions);
            
            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize event message from topic: {Topic}", topic);
                return;
            }
            
            _logger.LogInformation("Consumed event: Topic={Topic}, Type={Type}, Partition={Partition}, Offset={Offset}, Id={Id}",
                topic, @event.Type, partition, offset, @event.Id);
            
            if (topic == _topics["movie"])
            {
                await ProcessMovieEventAsync(@event);
            }
            else if (topic == _topics["user"])
            {
                await ProcessUserEventAsync(@event);
            }
            else if (topic == _topics["payment"])
            {
                await ProcessPaymentEventAsync(@event);
            }
            else
            {
                _logger.LogWarning("Unknown topic: {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message from topic: {Topic}", topic);
        }
    }

    private Task ProcessMovieEventAsync(Event @event)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<MovieEventPayload>(
                JsonSerializer.Serialize(@event.Payload), _jsonOptions);
            
            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize movie event payload");
                return Task.CompletedTask;
            }
            
            _logger.LogInformation("Movie Event: MovieId={MovieId}, Action={Action}, Title={Title}, UserId={UserId}",
                payload.MovieId, payload.Action, payload.Title, payload.UserId);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing movie event");
            return Task.CompletedTask;
        }
    }

    private Task ProcessUserEventAsync(Event @event)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<UserEventPayload>(
                JsonSerializer.Serialize(@event.Payload), _jsonOptions);
            
            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize user event payload");
                return Task.CompletedTask;
            }
            
            _logger.LogInformation("рџ‘¤ User Event: UserId={UserId}, Action={Action}, Username={Username}",
                payload.UserId, payload.Action, payload.Username);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user event");
            return Task.CompletedTask;
        }
    }

    private Task ProcessPaymentEventAsync(Event @event)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<PaymentEventPayload>(
                JsonSerializer.Serialize(@event.Payload), _jsonOptions);
            
            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize payment event payload");
                return Task.CompletedTask;
            }
            
            _logger.LogInformation("рџ’° Payment Event: PaymentId={PaymentId}, Amount={Amount}, Status={Status}, UserId={UserId}",
                payload.PaymentId, payload.Amount, payload.Status, payload.UserId);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment event");
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        foreach (var consumer in _consumers)
        {
            consumer?.Close();
            consumer?.Dispose();
        }
    }
}
