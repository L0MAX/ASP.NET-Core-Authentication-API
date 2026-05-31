namespace Application.Common.Interfaces;

public interface IPasswordResetTokenProvider
{
    Task<string> GenerateAndStoreTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid?> ValidateAndConsumeTokenAsync(string token, Guid userId, CancellationToken cancellationToken = default);
}
