using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

public sealed class TokenHasher : ITokenHasher
{
    private readonly byte[] _key;

    public TokenHasher(IOptions<TokenSecuritySettings> settings)
    {
        var hashSecret = settings.Value.HashSecret;

        if (string.IsNullOrWhiteSpace(hashSecret) || hashSecret.Length < 32)
        {
            throw new InvalidOperationException("TokenSecuritySettings:HashSecret must be at least 32 characters.");
        }

        _key = Encoding.UTF8.GetBytes(hashSecret);
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var tokenBytes = Encoding.UTF8.GetBytes(token);

        using var hmac = new HMACSHA256(_key);
        return Convert.ToBase64String(hmac.ComputeHash(tokenBytes));
    }
}
