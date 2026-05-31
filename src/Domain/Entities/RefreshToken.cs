using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a long-lived token used to obtain new access tokens without re-authenticating.
/// Owned by the <see cref="User"/> aggregate.
/// </summary>
public class RefreshToken : Entity
{
    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public Guid UserId { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;

    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
    }

    internal static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Refresh token expiration must be in the future.", nameof(expiresAt));
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
