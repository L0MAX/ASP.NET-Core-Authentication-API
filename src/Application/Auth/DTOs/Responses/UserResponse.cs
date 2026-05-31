namespace Application.Auth.DTOs.Responses;

public class UserResponse
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool EmailConfirmed { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];
}
