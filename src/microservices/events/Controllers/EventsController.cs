using EventsService.Models;
using EventsService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IKafkaProducerService kafkaProducer, ILogger<EventsController> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }


    [HttpPost("movie")]
    public async Task<ActionResult<EventResponse>> CreateMovieEvent([FromBody] MovieEventRequest request)
    {
        try
        {
            var payload = new MovieEventPayload
            {
                MovieId = request.MovieId,
                Title = request.Title,
                Action = request.Action,
                UserId = request.UserId,
                Rating = request.Rating,
                Genres = request.Genres ?? new List<string>(),
                Description = request.Description
            };

            var result = await _kafkaProducer.ProduceMovieEventAsync(payload);

            _logger.LogInformation("Movie event created: MovieId={MovieId}, Action={Action}",
                request.MovieId, request.Action);

            return Created((string?)null, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create movie event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("user")]
    public async Task<ActionResult<EventResponse>> CreateUserEvent([FromBody] UserEventRequest request)
    {
        try
        {
            var payload = new UserEventPayload
            {
                UserId = request.UserId,
                Username = request.Username,
                Email = request.Email,
                Action = request.Action,
                Timestamp = request.Timestamp
            };

            var result = await _kafkaProducer.ProduceUserEventAsync(payload);

            _logger.LogInformation("User event created: UserId={UserId}, Action={Action}",
                request.UserId, request.Action);

            return Created((string?)null, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("payment")]
    public async Task<ActionResult<EventResponse>> CreatePaymentEvent([FromBody] PaymentEventRequest request)
    {
        try
        {
            var payload = new PaymentEventPayload
            {
                PaymentId = request.PaymentId,
                UserId = request.UserId,
                Amount = request.Amount,
                Status = request.Status,
                Timestamp = request.Timestamp,
                MethodType = request.MethodType
            };

            var result = await _kafkaProducer.ProducePaymentEventAsync(payload);

            _logger.LogInformation("Payment event created: PaymentId={PaymentId}, Amount={Amount}, Status={Status}",
                request.PaymentId, request.Amount, request.Status);

            return Created((string?)null, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] EventRequest request)
    {
        try
        {
            var topic = request.Type.ToLower() switch
            {
                "movie" => "movie-events",
                "user" => "user-events",
                "payment" => "payment-events",
                _ => throw new ArgumentException($"Unknown event type: {request.Type}")
            };

            var result = await _kafkaProducer.ProduceEventAsync(request.Type, request.Payload, topic);

            _logger.LogInformation("Generic event created: Type={Type}, Topic={Topic}", request.Type, topic);

            return Created((string?)null, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create generic event");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}