using System.Net;
using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Models;
using FluentValidation;

namespace Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage).ToList()),
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                notFound.Message,
                null as IReadOnlyList<string>),
            UnauthorizedException unauthorized => (
                HttpStatusCode.Unauthorized,
                unauthorized.Message,
                null),
            ConflictException conflict => (
                HttpStatusCode.Conflict,
                conflict.Message,
                null),
            UnauthorizedAccessException unauthorized => (
                HttpStatusCode.Unauthorized,
                unauthorized.Message,
                null),
            ArgumentException argument => (
                HttpStatusCode.BadRequest,
                argument.Message,
                null),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception for {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var responseBody = errors is { Count: > 0 }
            ? ApiResponse<object>.Fail($"{message} {string.Join(' ', errors)}")
            : ApiResponse<object>.Fail(message);

        await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
