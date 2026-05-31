using System.Diagnostics;
using Api.Constants;

namespace Api.Extensions;

public static class HttpContextExtensions
{
    public static string GetTraceId(this HttpContext context)
    {
        if (context.Items.TryGetValue(HttpContextItemKeys.TraceId, out var traceId) && traceId is string id)
        {
            return id;
        }

        return Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
