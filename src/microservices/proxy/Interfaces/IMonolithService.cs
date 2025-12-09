using CinemaAbyss.Proxy.Models;
using Refit;

namespace CinemaAbyss.Proxy.Interfaces;

// Monolith Service Interface
public interface IMonolithService
{
    [Get("/health")]
    Task<IApiResponse<object>> HealthCheck();

    [Get("/api/users")]
    Task<IApiResponse<List<User>>> GetUsers();

    [Get("/api/users?id={id}")]
    Task<IApiResponse<User>> GetUserById(int id);

    [Post("/api/users")]
    Task<IApiResponse<User>> CreateUser([Body] User user);

    [Get("/api/movies")]
    Task<IApiResponse<List<Movie>>> GetMovies();

    [Get("/api/movies?id={id}")]
    Task<IApiResponse<Movie>> GetMovieById(int id);

    [Post("/api/movies")]
    Task<IApiResponse<Movie>> CreateMovie([Body] Movie movie);

    [Get("/api/payments")]
    Task<IApiResponse<List<Payment>>> GetPayments();

    [Get("/api/payments?id={id}")]
    Task<IApiResponse<Payment>> GetPaymentById(int id);

    [Post("/api/payments")]
    Task<IApiResponse<Payment>> CreatePayment([Body] Payment payment);

    [Get("/api/subscriptions")]
    Task<IApiResponse<List<Subscription>>> GetSubscriptions();

    [Get("/api/subscriptions?id={id}")]
    Task<IApiResponse<Subscription>> GetSubscriptionById(int id);

    [Post("/api/subscriptions")]
    Task<IApiResponse<Subscription>> CreateSubscription([Body] Subscription subscription);
}

