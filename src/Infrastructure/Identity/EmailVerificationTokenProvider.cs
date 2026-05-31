using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

public sealed class EmailVerificationTokenProvider : IEmailVerificationTokenProvider
{
    private readonly IEmailVerificationTokenRepository _repository;
    private readonly ITokenHasher _tokenHasher;
    private readonly EmailVerificationSettings _settings;

    public EmailVerificationTokenProvider(
        IEmailVerificationTokenRepository repository,
        ITokenHasher tokenHasher,
        IOptions<EmailVerificationSettings> settings)
    {
        _repository = repository;
        _tokenHasher = tokenHasher;
        _settings = settings.Value;
    }

    public async Task<string> GenerateAndStoreTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await InvalidateUserTokensAsync(userId, cancellationToken);

        var plainToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var tokenHash = _tokenHasher.Hash(plainToken);
        var expiresAt = DateTime.UtcNow.AddHours(_settings.TokenExpirationHours);

        var entity = EmailVerificationToken.Create(userId, tokenHash, expiresAt);
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return plainToken;
    }

    public async Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _repository.InvalidateAllForUserAsync(userId, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> ValidateAndConsumeTokenAsync(
        string token,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = _tokenHasher.Hash(token);
        var storedToken = await _repository.GetValidByHashAsync(tokenHash, userId, cancellationToken);

        if (storedToken is null)
        {
            return null;
        }

        storedToken.MarkUsed();
        await _repository.SaveChangesAsync(cancellationToken);

        return userId;
    }
}
