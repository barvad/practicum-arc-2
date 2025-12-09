using CinemaAbyss.Proxy.Models;
using Refit;

namespace CinemaAbyss.Proxy.Interfaces;

public interface IEventsService
{
    [Get("/api/events/health")]
    Task<IApiResponse<object>> HealthCheck();

    [Post("/api/events/movie")]
    Task<IApiResponse<object>> CreateMovieEvent([Body] MovieEvent movieEvent);

    [Post("/api/events/user")]
    Task<IApiResponse<object>> CreateUserEvent([Body] UserEvent userEvent);

    [Post("/api/events/payment")]
    Task<IApiResponse<object>> CreatePaymentEvent([Body] PaymentEvent paymentEvent);
}