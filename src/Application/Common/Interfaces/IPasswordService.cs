namespace Application.Common.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Returns a new hash when the stored hash uses outdated parameters; otherwise null.
    /// </summary>
    string? GetUpgradedHashIfNeeded(string password, string passwordHash);
}
