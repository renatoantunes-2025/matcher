namespace MatchR.Api.Services;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MatchR";
    public string Audience { get; set; } = "MatchR";
    public int ExpiryMinutes { get; set; } = 480;
}
