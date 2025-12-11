using System.Text.Json.Serialization;

namespace CinemaAbyss.Proxy.Models;

public class Subscription
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("plan_type")]
    public string PlanType { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}