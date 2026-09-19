namespace UrlShortener.WebApi.Middlewares;

using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // FluentValidation Handling
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                status = 400,
                title = "Validation Failed",
                errors = validationException.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
            }, cancellationToken);

            return true;
        }

        // Internal Server Error Fallback
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 500,
            title = "Internal Server Error",
            detail = "An unexpected error occurred processing your request."
        }, cancellationToken);

        return true;
    }
}