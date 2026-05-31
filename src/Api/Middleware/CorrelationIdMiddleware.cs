using Api.Constants;
using Api.Extensions;
using Serilog.Context;

namespace Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = ResolveTraceId(context);
        context.Items[HttpContextItemKeys.TraceId] = traceId;
        context.Response.Headers[CorrelationHeaderName] = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }

    private static string ResolveTraceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationHeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return context.TraceIdentifier;
    }
}
