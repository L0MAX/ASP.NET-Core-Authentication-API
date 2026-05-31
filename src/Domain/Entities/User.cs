using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Aggregate root for authentication. Owns refresh tokens and role assignments.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public bool EmailConfirmed { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = [];

    private readonly List<Role> _roles = [];

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private User()
    {
    }

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            EmailConfirmed = false
        };

        user.ApplyCreated(DateTime.UtcNow);
        return user;
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name is required.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        ApplyUpdated(DateTime.UtcNow);
    }

    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
        ApplyUpdated(DateTime.UtcNow);
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
        {
            return;
        }

        EmailConfirmed = true;
        ApplyUpdated(DateTime.UtcNow);
    }

    public RefreshToken IssueRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = RefreshToken.Create(Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        ApplyUpdated(DateTime.UtcNow);
        return refreshToken;
    }

    public void RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);

        if (refreshToken is null)
        {
            throw new DomainException("Refresh token not found.");
        }

        refreshToken.Revoke();
        ApplyUpdated(DateTime.UtcNow);
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var refreshToken in _refreshTokens.Where(t => t.IsActive))
        {
            refreshToken.Revoke();
        }

        ApplyUpdated(DateTime.UtcNow);
    }

    public void AssignRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (_roles.Any(r => r.Id == role.Id))
        {
            return;
        }

        _roles.Add(role);
        ApplyUpdated(DateTime.UtcNow);
    }

    public void RemoveRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        var existing = _roles.FirstOrDefault(r => r.Id == role.Id);

        if (existing is null)
        {
            return;
        }

        _roles.Remove(existing);
        ApplyUpdated(DateTime.UtcNow);
    }

    public bool HasRole(string roleName)
    {
        return _roles.Any(r =>
            string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }
}
