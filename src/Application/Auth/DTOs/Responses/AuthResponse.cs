namespace Application.Auth.DTOs.Responses;

public class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; init; }

    public UserResponse User { get; init; } = null!;
}
