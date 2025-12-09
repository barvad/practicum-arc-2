namespace CinemaAbyss.Proxy.Models;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}