namespace Application.Common.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Returns a new hash when the stored hash uses outdated parameters; otherwise null.
    /// </summary>
    string? GetUpgradedHashIfNeeded(string password, string passwordHash);

    /// <summary>
    /// Performs a constant-time password verification against a dummy hash to mitigate user-enumeration timing attacks.
    /// </summary>
    void RunDummyVerification(string password);
}
