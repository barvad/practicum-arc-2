using CinemaAbyss.Proxy.Interfaces;
using CinemaAbyss.Proxy.Models;
using Refit;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Read configuration from environment variables with fallback to appsettings
var monolithUrl = Environment.GetEnvironmentVariable("MONOLITH_URL")
    ?? builder.Configuration["Services:Monolith:BaseUrl"]
    ?? "http://localhost:8080";

var moviesUrl = Environment.GetEnvironmentVariable("MOVIES_SERVICE_URL")
    ?? builder.Configuration["Services:Movies:BaseUrl"]
    ?? "http://localhost:8081";

var eventsUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL")
    ?? builder.Configuration["Services:Events:BaseUrl"]
    ?? "http://localhost:8082";

var gradualMigration = Environment.GetEnvironmentVariable("GRADUAL_MIGRATION")?.ToLower() == "true"
    || (builder.Configuration["Migration:GradualMigration"]?.ToLower() == "true");

var migrationPercentStr = Environment.GetEnvironmentVariable("MOVIES_MIGRATION_PERCENT")
    ?? builder.Configuration["Migration:MoviesMigrationPercent"]
    ?? "50";
int.TryParse(migrationPercentStr, out int migrationPercent);

// Configure Refit clients
builder.Services.AddRefitClient<IMonolithService>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(monolithUrl));

builder.Services.AddRefitClient<IMoviesService>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(moviesUrl));

builder.Services.AddRefitClient<IEventsService>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(eventsUrl));

var app = builder.Build();



// Health check endpoint
app.MapGet("/health", () => new
{
    status = "healthy",
    service = "proxy",
    timestamp = DateTime.UtcNow,
    configuration = new
    {
        monolithUrl,
        moviesUrl,
        eventsUrl,
        gradualMigration,
        migrationPercent
    }
});

// Helper method to get service client
T GetService<T>(IServiceProvider services) => services.GetRequiredService<T>();


var random = new Random();

bool ShouldRouteToMoviesService()
{
    if (!gradualMigration) return true;

    
    var next = random.Next(100);
    return next < migrationPercent;
}

// Users endpoints (always to monolith)
app.MapGet("/api/users", async (HttpContext context, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.GetUsers();
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapGet("/api/users/{id}", async (int id, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.GetUserById(id);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapPost("/api/users", async (User user, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.CreateUser(user);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

// Movies endpoints with canary routing
app.MapGet("/api/movies", async (HttpContext context, IServiceProvider services) =>
{
    var targetService = ShouldRouteToMoviesService() ? "movies" : "monolith";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] GET /api/movies → {targetService} ({migrationPercent}%)");

    if (targetService == "movies")
    {
        var moviesService = GetService<IMoviesService>(services);
        var response = await moviesService.GetMovies();
        return Results.Json(response.Content, statusCode: (int)response.StatusCode);
    }
    else
    {
        var monolith = GetService<IMonolithService>(services);
        var response = await monolith.GetMovies();
        return Results.Json(response.Content, statusCode: (int)response.StatusCode);
    }
});

app.MapGet("/api/movies/{id}", async (int id, IServiceProvider services) =>
{
    var targetService = ShouldRouteToMoviesService() ? "movies" : "monolith";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] GET /api/movies/{id} → {targetService} ({migrationPercent}%)");

    if (targetService == "movies")
    {
        var moviesService = GetService<IMoviesService>(services);
        var response = await moviesService.GetMovieById(id);
        return Results.Json(response.Content, statusCode: (int)response.StatusCode);
    }
    else
    {
        var monolith = GetService<IMonolithService>(services);
        var response = await monolith.GetMovieById(id);
        return Results.Json(response.Content, statusCode: (int)response.StatusCode);
    }
});

app.MapPost("/api/movies", async (Movie movie, IServiceProvider services) =>
{
    var isMoviesService = ShouldRouteToMoviesService();
    var targetService = isMoviesService  ? "monolith" : "movies";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] POST /api/movies → {targetService}");

    if (isMoviesService)
    {
        var moviesService = GetService<IMoviesService>(services);
        var response = await moviesService.CreateMovie(movie);
        return Results.Json(response.Content, statusCode: (int)response.StatusCode);
    }
    else
    {
        var monolith = GetService<IMonolithService>(services);
        var response = await monolith.CreateMovie(movie);
        return Results.Json(response.Content, statusCode: (int)response.StatusCode);
    }
});

// Payments endpoints (always to monolith)
app.MapGet("/api/payments", async (IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.GetPayments();
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapGet("/api/payments/{id}", async (int id, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.GetPaymentById(id);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapPost("/api/payments", async (Payment payment, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.CreatePayment(payment);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

// Subscriptions endpoints (always to monolith)
app.MapGet("/api/subscriptions", async (IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.GetSubscriptions();
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapGet("/api/subscriptions/{id}", async (int id, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.GetSubscriptionById(id);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapPost("/api/subscriptions", async (Subscription subscription, IServiceProvider services) =>
{
    var monolith = GetService<IMonolithService>(services);
    var response = await monolith.CreateSubscription(subscription);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

// Events endpoints (always to events service)
app.MapGet("/api/events/health", async (IServiceProvider services) =>
{
    var eventsService = GetService<IEventsService>(services);
    var response = await eventsService.HealthCheck();
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapPost("/api/events/movie", async (MovieEvent movieEvent, IServiceProvider services) =>
{
    var eventsService = GetService<IEventsService>(services);
    var response = await eventsService.CreateMovieEvent(movieEvent);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapPost("/api/events/user", async (UserEvent userEvent, IServiceProvider services) =>
{
    var eventsService = GetService<IEventsService>(services);
    var response = await eventsService.CreateUserEvent(userEvent);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

app.MapPost("/api/events/payment", async (PaymentEvent paymentEvent, IServiceProvider services) =>
{
    var eventsService = GetService<IEventsService>(services);
    var response = await eventsService.CreatePaymentEvent(paymentEvent);
    return Results.Json(response.Content, statusCode: (int)response.StatusCode);
});

// Fallback route for all other requests to monolith
app.MapFallback(async (HttpContext context, IServiceProvider services) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {context.Request.Method} {context.Request.Path} → Monolith (fallback)");

    var monolith = GetService<IMonolithService>(services);

    // Forward the request
    var client = new HttpClient { BaseAddress = new Uri(monolithUrl) };
    var request = new HttpRequestMessage(
        new HttpMethod(context.Request.Method),
        context.Request.Path + context.Request.QueryString
    );

    // Copy headers
    foreach (var header in context.Request.Headers)
    {
        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    // Copy body for POST, PUT, PATCH
    if (context.Request.ContentLength > 0)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
    }

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();

    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
    await context.Response.WriteAsync(content);
});

app.Run();