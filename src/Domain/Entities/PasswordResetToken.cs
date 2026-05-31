using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// One-time password reset token owned by a user.
/// </summary>
public class PasswordResetToken : Entity
{
    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsUsed { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsValid => !IsUsed && !IsExpired;

    private PasswordResetToken()
    {
    }

    internal static PasswordResetToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Token expiration must be in the future.", nameof(expiresAt));
        }

        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsUsed = false
        };
    }

    public void MarkUsed()
    {
        IsUsed = true;
    }
}
