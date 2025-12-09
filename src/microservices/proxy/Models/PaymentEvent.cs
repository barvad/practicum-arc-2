namespace CinemaAbyss.Proxy.Models;

public class PaymentEvent
{
    public int PaymentId { get; set; }
    public int UserId { get; set; }
    public double Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string MethodType { get; set; } = string.Empty;
}