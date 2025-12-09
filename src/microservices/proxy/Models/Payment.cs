namespace CinemaAbyss.Proxy.Models;

public class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public double Amount { get; set; }
    public DateTime Timestamp { get; set; }
}