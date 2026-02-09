namespace MarketList.Infrastructure.Configurations;

public class ApiOptions
{
    // Base URL of this API (useful for frontend or external integrations)
    public string BaseUrl { get; set; } = string.Empty;

    // Allowed origins for CORS
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
