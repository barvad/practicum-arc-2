namespace CinemaAbyss.Proxy.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public double Rating { get; set; }
}