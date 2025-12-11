using CinemaAbyss.Proxy.Models;
using Refit;

namespace CinemaAbyss.Proxy.Interfaces;

public interface IMoviesService
{
    [Get("/api/movies/health")]
    Task<IApiResponse<object>> HealthCheck();

    [Get("/api/movies")]
    Task<IApiResponse<List<Movie>>> GetMovies();

    [Get("/api/movies?id={id}")]
    Task<IApiResponse<Movie>> GetMovieById(int id);

    [Post("/api/movies")]
    Task<IApiResponse<Movie>> CreateMovie([Body] Movie movie);
}