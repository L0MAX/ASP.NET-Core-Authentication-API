namespace Application.Auth.DTOs.Requests;

public class LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
