using System.Net;
using System.Text.Json;
using CatalogueService.Exceptions;

namespace CatalogueService.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message),
            ConflictException e => (HttpStatusCode.Conflict, e.Message),
            UnauthorizedException e => (HttpStatusCode.Unauthorized, e.Message),
            ForbiddenException e => (HttpStatusCode.Forbidden, e.Message),
            ArgumentException e => (HttpStatusCode.BadRequest, e.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            error = message,
            correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown",
            timestamp = DateTime.UtcNow
        }));
    }
}
