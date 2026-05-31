using Application.Common.Interfaces;
using BCrypt.Net;

namespace Infrastructure.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) =>
        BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string passwordHash) =>
        BCrypt.Verify(password, passwordHash);
}
