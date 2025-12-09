using System.Text.Json.Serialization;

namespace CinemaAbyss.Proxy.Models;

public class MovieEvent
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("movie_id")]
    public int MovieId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
}