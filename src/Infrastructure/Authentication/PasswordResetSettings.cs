namespace Infrastructure.Authentication;

public class PasswordResetSettings
{
    public const string SectionName = "PasswordResetSettings";

    /// <summary>
    /// Reset token lifetime in hours. OWASP recommends short-lived tokens (default: 1 hour).
    /// </summary>
    public int TokenExpirationHours { get; set; } = 1;

    /// <summary>
    /// Base URL for the password reset page (token and email appended as query params).
    /// </summary>
    public string ResetLinkBaseUrl { get; set; } = "https://localhost:5001/reset-password";
}
