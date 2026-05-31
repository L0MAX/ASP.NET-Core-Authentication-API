namespace Infrastructure.Authentication;

public class EmailVerificationSettings
{
    public const string SectionName = "EmailVerificationSettings";

    /// <summary>
    /// Token lifetime in hours. Default: 24 hours.
    /// </summary>
    public int TokenExpirationHours { get; set; } = 24;
}
