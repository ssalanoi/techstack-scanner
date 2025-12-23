namespace Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TechStackScanner.Api";

    public string Audience { get; set; } = "TechStackScanner.Web";

    public int AccessTokenMinutes { get; set; } = 60;

    public string Secret { get; set; } = string.Empty;
}
