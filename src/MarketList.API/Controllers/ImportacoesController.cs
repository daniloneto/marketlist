using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/importacoes")]
public class ImportacoesController : ControllerBase
{
    private readonly INotaFiscalCrawlerService _crawlerService;
    private readonly IEmpresaResolverService _empresaResolver;
    private readonly IListaDeComprasService _listaService;
    private readonly ILogger<ImportacoesController> _logger;

    public ImportacoesController(
        INotaFiscalCrawlerService crawlerService,
        IEmpresaResolverService empresaResolver,
        IListaDeComprasService listaService,
        ILogger<ImportacoesController> logger)
    {
        _crawlerService = crawlerService;
        _empresaResolver = empresaResolver;
        _listaService = listaService;
        _logger = logger;
    }

    /// <summary>
    /// Importa uma nota fiscal a partir da URL do QR Code da NFC-e.
    /// O crawler baixa o HTML da SEFAZ, extrai empresa e itens,
    /// converte para TEXTO e envia ao pipeline existente de importação.
    /// </summary>
    [HttpPost("nota-fiscal/qrcode")]
    public async Task<IActionResult> ImportarPorQrCode(
        [FromBody] ImportarQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { success = false, message = "A URL do QR Code é obrigatória." });
        }

        _logger.LogInformation("Recebida solicitação de importação por QR Code: {Url}", request.Url);

        try
        {
            // 1. Baixar HTML e extrair texto (empresa + itens) e data de emissão
            var notaExtraida = await _crawlerService.BaixarEExtrairTextoAsync(request.Url, cancellationToken);

            // 2. Separar nome da empresa (primeira linha) do texto dos itens
            var linhas = notaExtraida.Texto.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
            var nomeEmpresa = linhas[0].Trim();
            var textoItens = string.Join("\n", linhas.Skip(1)).Trim();

            // 3. Resolver empresa por nome (fuzzy match)
            var empresaId = await _empresaResolver.ResolverEmpresaIdPorNomeAsync(nomeEmpresa, cancellationToken);

            // 4. Criar lista via pipeline existente (enfileira Hangfire automaticamente)
            //    Usa a data de emissão da NFC-e em vez de DateTime.UtcNow
            var nomeLista = $"QR Code - {nomeEmpresa} - {notaExtraida.DataEmissao:yyyy-MM-dd HH:mm:ss}";
            var createDto = new ListaDeComprasCreateDto(
                nomeLista,
                textoItens,
                TipoEntrada.NotaFiscal,
                empresaId,
                notaExtraida.DataEmissao
            );

            var lista = await _listaService.CreateAsync(createDto, cancellationToken);

            _logger.LogInformation(
                "Lista criada a partir de QR Code: {ListaId}, Empresa: {Empresa} (Id: {EmpresaId})",
                lista.Id, nomeEmpresa, empresaId);

            return Ok(new
            {
                success = true,
                message = $"Nota fiscal importada com sucesso. Empresa: {nomeEmpresa}, processamento em andamento.",
                listaId = lista.Id,
                empresa = nomeEmpresa,
                empresaId
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha na importação por QR Code: {Url}", request.Url);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado na importação por QR Code: {Url}", request.Url);
            return StatusCode(500, new { success = false, message = "Erro interno ao processar a nota fiscal." });
        }
    }
}

/// <summary>
/// Request body para importação de nota fiscal por QR Code.
/// </summary>
public record ImportarQrCodeRequest(string Url);
