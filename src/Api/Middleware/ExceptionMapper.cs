using System.Net;
using System.Text.Json;
using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Models;
using Domain.Exceptions;
using FluentValidation;

namespace Api.Middleware;

internal sealed record ExceptionHandlingResult(
    HttpStatusCode StatusCode,
    string Message,
    string ErrorCode,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Details = null);

internal static class ExceptionMapper
{
    public static ExceptionHandlingResult Map(Exception exception, bool isDevelopment)
    {
        return exception switch
        {
            ValidationException validation => new ExceptionHandlingResult(
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                ErrorCodes.ValidationFailed,
                ValidationErrorMapper.Map(validation.Errors)),

            NotFoundException notFound => new ExceptionHandlingResult(
                HttpStatusCode.NotFound,
                notFound.Message,
                ErrorCodes.NotFound),

            UnauthorizedException unauthorized => new ExceptionHandlingResult(
                HttpStatusCode.Unauthorized,
                unauthorized.Message,
                ErrorCodes.Unauthorized),

            ForbiddenException forbidden => new ExceptionHandlingResult(
                HttpStatusCode.Forbidden,
                forbidden.Message,
                ErrorCodes.Forbidden),

            ConflictException conflict => new ExceptionHandlingResult(
                HttpStatusCode.Conflict,
                conflict.Message,
                ErrorCodes.Conflict),

            DomainException domain => new ExceptionHandlingResult(
                HttpStatusCode.BadRequest,
                domain.Message,
                ErrorCodes.DomainRuleViolation),

            UnauthorizedAccessException unauthorizedAccess => new ExceptionHandlingResult(
                HttpStatusCode.Unauthorized,
                unauthorizedAccess.Message,
                ErrorCodes.Unauthorized),

            ArgumentException argument => new ExceptionHandlingResult(
                HttpStatusCode.BadRequest,
                argument.Message,
                ErrorCodes.InvalidArgument),

            BadHttpRequestException badRequest => new ExceptionHandlingResult(
                HttpStatusCode.BadRequest,
                badRequest.Message,
                ErrorCodes.BadRequest),

            JsonException json => new ExceptionHandlingResult(
                HttpStatusCode.BadRequest,
                "The request body contains invalid JSON.",
                ErrorCodes.InvalidJson,
                Details: isDevelopment ? json.Message : null),

            _ => new ExceptionHandlingResult(
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.",
                ErrorCodes.InternalError,
                Details: isDevelopment ? exception.ToString() : null)
        };
    }
}
