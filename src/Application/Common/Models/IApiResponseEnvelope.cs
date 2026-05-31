namespace Application.Common.Models;

public interface IApiResponseEnvelope
{
    bool Success { get; }

    object Enrich(string traceId, string? inferredErrorCode);
}
