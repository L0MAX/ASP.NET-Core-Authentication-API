using System.Security.Cryptography;
using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Identity;

public sealed class EmailVerificationTokenProvider : IEmailVerificationTokenProvider
{
    private const string CacheKeyPrefix = "email-verification:";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IMemoryCache _cache;

    public EmailVerificationTokenProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<string> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _cache.Set($"{CacheKeyPrefix}{token}", userId, TokenLifetime);
        return Task.FromResult(token);
    }

    public Task<Guid?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue($"{CacheKeyPrefix}{token}", out Guid userId))
        {
            _cache.Remove($"{CacheKeyPrefix}{token}");
            return Task.FromResult<Guid?>(userId);
        }

        return Task.FromResult<Guid?>(null);
    }
}
