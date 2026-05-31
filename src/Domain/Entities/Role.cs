using Domain.Common;
using Domain.Constants;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Represents a named authorization role assigned to users.
/// </summary>
public class Role : Entity
{
    public string Name { get; private set; } = null!;

    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    private Role()
    {
    }

    public static Role Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }

        var normalizedName = name.Trim();

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = normalizedName
        };
    }

    public static Role CreateUser() => Create(RoleNames.User);

    public static Role CreateAdmin() => Create(RoleNames.Admin);

    internal static Role CreateForSeed(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }

        return new Role
        {
            Id = id,
            Name = name.Trim()
        };
    }
}
