namespace MarketList.Infrastructure.Configurations;

public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public string[] ChatIdsPermitidos { get; set; } = Array.Empty<string>();
}
