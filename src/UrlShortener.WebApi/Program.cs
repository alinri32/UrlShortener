using Microsoft.OpenApi;
using System.Security.Claims;
using System.Threading.RateLimiting;
using UrlShortener.Application;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Services;
using UrlShortener.Infrastructure;
using UrlShortener.WebApi.Authentication;
using UrlShortener.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Dependency Injection Setup
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Problem Details & Exception Handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Endpoints API Explorer & Swagger Setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "URL Shortener API",
        Version = "V1",
        Description = "Microservice URL Shortener"
    });


    // API Key Definition
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Enter your API Key."
    });

    // Add Security Requirement
    options.AddSecurityRequirement(doc =>
    {
        var requirement = new OpenApiSecurityRequirement();

        var apiKeyRef = new OpenApiSecuritySchemeReference("ApiKey", doc);

        requirement.Add(apiKeyRef, []);

        return requirement;
    });
});

// API Key Auth Setup
builder.Services.AddAuthorization();

// High-Throughput Fixed Window Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("CreationPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Swagger Middleware (Enabled for Development & Testing)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "URL Shortener API v1");
        c.RoutePrefix = "swagger";
    });
}

// Middleware Pipeline
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthorization();

// Command Route: Create Short URL
app.MapPost("/api/v1/urls", async (
    ShortenUrlRequest request,
    IUrlService urlService,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    long? userId = null;

    if (httpContext.User.Identity?.IsAuthenticated == true)
    {
        var claimValue = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(claimValue, out long parsedId))
        {
            userId = parsedId;
        }
    }
    else if (httpContext.Items.TryGetValue("UserId", out var idObj) && idObj is long id)
    {
        userId = id;
    }

    string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var response = await urlService.SetAsync(request, userId, baseUrl, ct);

    return Results.Created($"/{response.ShortCode}", response);
})
.WithName("CreateShortUrl")
.WithSummary("Creates a new shortened URL")
.Produces<ShortenUrlResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status429TooManyRequests)
.RequireRateLimiting("CreationPolicy")
.AddEndpointFilter<ApiKeyEndpointFilter>();

// Query Route: Redirect
app.MapGet("/{shortCode:regex(^[a-zA-Z0-9]{{1,11}}$)}", async (
    string shortCode,
    IUrlService urlService,
    CancellationToken ct) =>
{
    string? originalUrl = await urlService.GetAsync(shortCode, ct);

    return originalUrl is not null
        ? Results.Redirect(originalUrl, permanent: false)
        : Results.NotFound();
})
.WithName("RedirectUrl")
.WithSummary("Redirects to the original URL via HTTP 302")
.Produces(StatusCodes.Status302Found)
.Produces(StatusCodes.Status404NotFound);

app.Run();