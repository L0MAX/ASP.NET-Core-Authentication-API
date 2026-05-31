namespace Api.Configuration;

public class CorsSettings
{
    public const string SectionName = "CorsSettings";

    public const string DefaultPolicyName = "Default";

    public string[] AllowedOrigins { get; set; } = [];
}
