using FeedService.Exceptions;
using System.Net;
using System.Text.Json;
using FeedService.Exceptions;

namespace FeedService.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger) { _next = next; _logger = logger; }
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            var (code, msg) = ex switch
            {
                NotFoundException e => (HttpStatusCode.NotFound, e.Message),
                UnauthorizedException e => (HttpStatusCode.Unauthorized, e.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)code;
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = (int)code,
                error = msg,
                correlationId = ctx.Items["CorrelationId"]?.ToString(),
                timestamp = DateTime.UtcNow
            }));
        }
    }
}