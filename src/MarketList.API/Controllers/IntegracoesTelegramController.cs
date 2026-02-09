using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Infrastructure.Configurations;
using MarketList.Infrastructure.Services;
using MarketList.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/integracoes/telegram")]
public class IntegracoesTelegramController : ControllerBase
{
    private readonly IListaDeComprasService _listaService;
    private readonly ITelegramClientService _telegramClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<IntegracoesTelegramController> _logger;
    private readonly IEmpresaResolverService _empresaResolver;

    public IntegracoesTelegramController(
        IListaDeComprasService listaService,
        ITelegramClientService telegramClient,
        IOptions<TelegramOptions> options,
        ILogger<IntegracoesTelegramController> logger,
        IEmpresaResolverService empresaResolver)
    {
        _listaService = listaService;
        _telegramClient = telegramClient;
        _options = options.Value;
        _logger = logger;
        _empresaResolver = empresaResolver;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] object updatePayload, CancellationToken cancellationToken)
    {
        try
        {
            // Extrair chatId e texto de forma simples para evitar dependência de SDK
            var json = System.Text.Json.JsonDocument.Parse(updatePayload.ToString() ?? string.Empty);

            // Tenta obter message -> text
            if (!json.RootElement.TryGetProperty("message", out var messageEl))
            {
                _logger.LogInformation("Webhook do Telegram sem 'message' - ignorando");
                return Ok();
            }

            var chatId = 0L;
            if (messageEl.TryGetProperty("chat", out var chatEl) && chatEl.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var idVal))
            {
                chatId = idVal;
            }

            // Security: permitir apenas chatIds configurados
            if (!_options.ChatIdsPermitidos.Contains(chatId.ToString()))
            {
                _logger.LogWarning("ChatId não autorizado: {ChatId}", chatId);
                return Forbid();
            }

            string? texto = null;
            if (messageEl.TryGetProperty("text", out var textEl))
            {
                texto = textEl.GetString();
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                await _telegramClient.EnviarMensagemAsync(chatId, "Envie o texto da nota fiscal para importar", cancellationToken);
                return Ok();
            }

            // Separar primeira linha (nome da empresa) do restante da nota
            var lines = texto.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                              .Select(l => l.Trim()).ToArray();

            if (lines.Length < 2)
            {
                await _telegramClient.EnviarMensagemAsync(chatId, "Envie o nome da empresa na primeira linha e a nota fiscal abaixo.", cancellationToken);
                return Ok();
            }

            var nomeEmpresa = lines[0];
            var textoNotaFiscal = string.Join(System.Environment.NewLine, lines.Skip(1));

            _logger.LogInformation("Recebido nota do Telegram. Empresa: {Empresa} ChatId: {ChatId}", nomeEmpresa, chatId);

            // Resolver EmpresaId por nome
            var empresaId = await _empresaResolver.ResolverEmpresaIdPorNomeAsync(nomeEmpresa, cancellationToken);
            if (!empresaId.HasValue)
            {
                await _telegramClient.EnviarMensagemAsync(chatId, $"Empresa '{nomeEmpresa}' não encontrada. Cadastre a empresa no sistema antes de importar a nota.", cancellationToken);
                return Ok();
            }

            // Reutilizar serviço existente de criação de lista por texto
            var nomeLista = $"Importado do Telegram - {nomeEmpresa} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            var createDto = new ListaDeComprasCreateDto(nomeLista, textoNotaFiscal, TipoEntrada.NotaFiscal, empresaId);

            _ = await _listaService.CreateAsync(createDto, cancellationToken);

            // Responder ao usuário
            await _telegramClient.EnviarMensagemAsync(chatId, "Nota recebida e em processamento ✅", cancellationToken);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook do Telegram");
            return StatusCode(500);
        }
    }
}
