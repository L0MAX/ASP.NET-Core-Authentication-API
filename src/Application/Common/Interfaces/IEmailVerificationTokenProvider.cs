namespace Application.Common.Interfaces;

public interface IEmailVerificationTokenProvider
{
    Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
