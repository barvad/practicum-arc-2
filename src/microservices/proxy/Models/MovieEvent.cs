namespace CinemaAbyss.Proxy.Models;

public class MovieEvent
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int UserId { get; set; }
}