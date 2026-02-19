using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/importacoes")]
public class ImportacoesController : ControllerBase
{
    private readonly IImportacaoNotaFiscalService _importacaoNotaFiscalService;
    private readonly ILogger<ImportacoesController> _logger;

    public ImportacoesController(
        IImportacaoNotaFiscalService importacaoNotaFiscalService,
        ILogger<ImportacoesController> logger)
    {
        _importacaoNotaFiscalService = importacaoNotaFiscalService;
        _logger = logger;
    }

    [HttpPost("nota-fiscal/qrcode")]
    public Task<IActionResult> ImportarPorQrCode(
        [FromBody] ImportarQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        return ImportarPorUrlInternoAsync(request.Url, "QR Code", cancellationToken);
    }

    [HttpPost("nota-fiscal/por-url")]
    public Task<IActionResult> ImportarPorUrl(
        [FromBody] ImportarNotaPorUrlRequest request,
        CancellationToken cancellationToken)
    {
        return ImportarPorUrlInternoAsync(request.UrlNota, "Endereço", cancellationToken);
    }

    private async Task<IActionResult> ImportarPorUrlInternoAsync(
        string? urlNota,
        string origem,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(urlNota))
        {
            return BadRequest(new { success = false, message = "O endereço da nota fiscal é obrigatório." });
        }

        if (!Uri.TryCreate(urlNota, UriKind.Absolute, out _))
        {
            return BadRequest(new { success = false, message = "Informe um endereço de nota fiscal válido." });
        }

        _logger.LogInformation("Recebida solicitação de importação de nota por {Origem}: {Url}", origem, urlNota);

        try
        {
            var lista = await _importacaoNotaFiscalService.ImportarNotaPorUrlAsync(urlNota, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Nota recebida e em processamento",
                listaId = lista.Id
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha controlada na importação de nota por {Origem}: {Url}", origem, urlNota);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado na importação de nota por {Origem}: {Url}", origem, urlNota);
            return StatusCode(500, new { success = false, message = "Erro interno ao processar a nota fiscal." });
        }
    }
}

public record ImportarQrCodeRequest(string Url);

public record ImportarNotaPorUrlRequest(string UrlNota);
