using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// Password hashing using ASP.NET Core Identity's PBKDF2-based PasswordHasher.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    private static readonly string DummyPasswordHash = new PasswordHasher<User>()
        .HashPassword(user: null!, "DummyTimingPlaceholder1!");

    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _hasher.HashPassword(user: null!, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(user: null!, passwordHash, password);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public string? GetUpgradedHashIfNeeded(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(user: null!, passwordHash, password);

        return result switch
        {
            PasswordVerificationResult.SuccessRehashNeeded => HashPassword(password),
            _ => null
        };
    }

    public void RunDummyVerification(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        _hasher.VerifyHashedPassword(user: null!, DummyPasswordHash, password);
    }
}
