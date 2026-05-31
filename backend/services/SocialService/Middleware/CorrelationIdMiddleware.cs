namespace SocialService.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue("X-Correlation-ID", out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString() : Guid.NewGuid().ToString();
        ctx.Items["CorrelationId"] = id;
        ctx.Response.Headers["X-Correlation-ID"] = id;
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))
            await _next(ctx);
    }
}