using System.Text.Json;
using Api.Extensions;
using Application.Common.Constants;
using Application.Common.Models;
using Serilog.Context;

namespace Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Request cancelled for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(
                exception,
                "Exception thrown after response started for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            throw exception;
        }

        var traceId = context.GetTraceId();
        var result = ExceptionMapper.Map(exception, _environment.IsDevelopment());

        LogException(context, exception, result, traceId);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)result.StatusCode;
        context.Response.Headers["X-Correlation-ID"] = traceId;

        var responseBody = ApiResponse<object>.Fail(
            result.Message,
            result.ErrorCode,
            result.Errors,
            traceId,
            result.Details);

        await context.Response.WriteAsJsonAsync(responseBody, SerializerOptions, context.RequestAborted);
    }

    private void LogException(
        HttpContext context,
        Exception exception,
        ExceptionHandlingResult result,
        string traceId)
    {
        var statusCode = (int)result.StatusCode;
        var isServerError = statusCode >= StatusCodes.Status500InternalServerError;
        var isValidationError = result.ErrorCode == ErrorCodes.ValidationFailed;

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("ErrorCode", result.ErrorCode))
        using (LogContext.PushProperty("StatusCode", statusCode))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            if (isServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                return;
            }

            if (isValidationError)
            {
                _logger.LogInformation(
                    "Validation failed for {Method} {Path} with {ValidationErrorCount} field errors",
                    context.Request.Method,
                    context.Request.Path,
                    result.Errors?.Count ?? 0);
                return;
            }

            _logger.LogWarning(
                exception,
                "Handled {ErrorCode} for {Method} {Path}: {Message}",
                result.ErrorCode,
                context.Request.Method,
                context.Request.Path,
                result.Message);
        }
    }
}
