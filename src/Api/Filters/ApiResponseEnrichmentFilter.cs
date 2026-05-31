using Api.Extensions;
using Application.Common.Constants;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public sealed class ApiResponseEnrichmentFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value is IApiResponseEnvelope envelope)
        {
            var traceId = context.HttpContext.GetTraceId();
            var inferredErrorCode = envelope.Success
                ? null
                : InferErrorCode(objectResult.StatusCode ?? context.HttpContext.Response.StatusCode);

            objectResult.Value = envelope.Enrich(traceId, inferredErrorCode);
        }

        await next();
    }

    private static string InferErrorCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthorized,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
        _ => ErrorCodes.InternalError
    };
}
