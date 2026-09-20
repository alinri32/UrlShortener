using UrlShortener.Application;
using UrlShortener.Infrastructure;
using UrlShortener.WebApi.Endpoints;
using UrlShortener.WebApi.Extensions;
using UrlShortener.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Dependency Injection Setup
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Cross-Cutting Concerns Setup
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAuthorization();
builder.Services.AddRateLimiterConfiguration();
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

// Middleware Pipeline
app.UseSwaggerConfiguration();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthorization();

// Route Registration
app.MapUrlEndpoints();

app.Run();