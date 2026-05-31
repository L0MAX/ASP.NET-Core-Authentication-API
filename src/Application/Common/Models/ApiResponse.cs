namespace Application.Common.Models;

public sealed class ApiResponse<T> : IApiResponseEnvelope
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public string? ErrorCode { get; init; }

    public string? TraceId { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Additional diagnostic detail. Populated only in Development for unexpected errors.
    /// </summary>
    public string? Details { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new()
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        };

    public static ApiResponse<T> Fail(
        string message,
        string? errorCode = null,
        IReadOnlyDictionary<string, string[]>? errors = null,
        string? traceId = null,
        string? details = null) =>
        new()
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
            TraceId = traceId,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow
        };

    public ApiResponse<T> WithRequestContext(string traceId) =>
        new()
        {
            Success = Success,
            Data = Data,
            Message = Message,
            Errors = Errors,
            ErrorCode = ErrorCode,
            TraceId = traceId,
            Details = Details,
            Timestamp = DateTimeOffset.UtcNow
        };

    object IApiResponseEnvelope.Enrich(string traceId, string? inferredErrorCode)
    {
        if (Success)
        {
            return WithRequestContext(traceId);
        }

        return Fail(
            Message ?? "An error occurred.",
            ErrorCode ?? inferredErrorCode,
            Errors,
            traceId,
            Details);
    }
}
