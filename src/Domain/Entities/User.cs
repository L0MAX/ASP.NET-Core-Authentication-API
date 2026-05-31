using Domain.Common;

namespace Domain.Entities;

public class User : BaseEntity
{
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;
}
