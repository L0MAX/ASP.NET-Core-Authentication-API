using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// Password hashing using ASP.NET Core Identity's PBKDF2-based PasswordHasher.
/// </summary>
public sealed class PasswordService : IPasswordService
{
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
}
