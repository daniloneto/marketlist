namespace MarketList.Infrastructure.Configurations;

public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public string[] ChatIdsPermitidos { get; set; } = Array.Empty<string>();

    // Base URL for Telegram API (ex: https://api.telegram.org)
    public string BaseUrl { get; set; } = string.Empty;

    // Path used for registering webhook in the API (ex: /api/integracoes/telegram/webhook)
    public string WebhookPath { get; set; } = string.Empty;

    // Timeout in seconds for Telegram HTTP requests
    public int TimeoutSegundos { get; set; } = 30;

    // Secret token required for incoming webhook requests (X-Telegram-Token or ?token=)
    public string WebhookToken { get; set; } = string.Empty;
}
