namespace Infrastructure.Authentication;

public class TokenSecuritySettings
{
    public const string SectionName = "TokenSecuritySettings";

    /// <summary>
    /// Dedicated secret for HMAC-hashing refresh, email-verification, and password-reset tokens.
    /// Must be separate from JwtSettings:Secret so JWT rotation does not invalidate stored tokens.
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;
}
