using MarketList.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace MarketList.Infrastructure.Services;

public interface ITelegramClientService
{
    Task EnviarMensagemAsync(long chatId, string texto, CancellationToken cancellationToken = default);
}

public class TelegramClientService : ITelegramClientService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;

    public TelegramClientService(HttpClient httpClient, IOptions<TelegramOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task EnviarMensagemAsync(long chatId, string texto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
            throw new InvalidOperationException("Telegram BotToken não configurado.");

        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = texto
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
