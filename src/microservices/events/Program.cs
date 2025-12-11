using System.Xml;
using EventsService;
using EventsService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();
builder.Services.AddHostedService<KafkaConsumerBackgroundService>();
builder.Services.AddHealthChecks();

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") 
    ?? builder.Configuration["Kafka:BootstrapServers"] 
    ?? "localhost:9092";

var port = Environment.GetEnvironmentVariable("PORT") 
    ?? "8082";

builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
 Task WriteCustomResponse(HttpContext httpContext, HealthReport result)
{
    httpContext.Response.ContentType = "application/json";

    var response = new
    {
        status = true
       
    };

    return httpContext.Response.WriteAsync(JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented));
}
app.UseHealthChecks("/api/events/health",new HealthCheckOptions(){ResponseWriter = WriteCustomResponse});
app.MapControllers();
app.Run();

namespace EventsService
{
    public class KafkaConsumerBackgroundService : BackgroundService
    {
        private readonly IKafkaConsumerService _kafkaConsumer;
        private readonly ILogger<KafkaConsumerBackgroundService> _logger;

        public KafkaConsumerBackgroundService(
            IKafkaConsumerService kafkaConsumer,
            ILogger<KafkaConsumerBackgroundService> logger)
        {
            _kafkaConsumer = kafkaConsumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Kafka consumer background service");
        
            try
            {
                await _kafkaConsumer.StartConsumingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Kafka consumer background service");
            }
        }
    }
}
