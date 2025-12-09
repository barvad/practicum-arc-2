namespace CinemaAbyss.Proxy.Models;

public class UserEvent
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}