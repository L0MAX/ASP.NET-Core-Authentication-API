using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

public sealed class EmailVerificationTokenProvider : IEmailVerificationTokenProvider
{
    private readonly IEmailVerificationTokenRepository _repository;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailVerificationSettings _settings;

    public EmailVerificationTokenProvider(
        IEmailVerificationTokenRepository repository,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EmailVerificationSettings> settings)
    {
        _repository = repository;
        _jwtSettings = jwtSettings.Value;
        _settings = settings.Value;
    }

    public async Task<string> GenerateAndStoreTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await InvalidateUserTokensAsync(userId, cancellationToken);

        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(plainToken);
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

        var tokenHash = HashToken(token);
        var storedToken = await _repository.GetValidByHashAsync(tokenHash, userId, cancellationToken);

        if (storedToken is null)
        {
            return null;
        }

        storedToken.MarkUsed();
        await _repository.SaveChangesAsync(cancellationToken);

        return userId;
    }

    private string HashToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
        var tokenBytes = Encoding.UTF8.GetBytes(token);

        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(tokenBytes));
    }
}
