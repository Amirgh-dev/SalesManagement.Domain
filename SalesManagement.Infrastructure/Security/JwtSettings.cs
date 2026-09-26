namespace SalesManagement.Infrastructure.Security
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 10080;
        public int RefreshIntervalMinutes { get; set; } = 1440;
    }
}