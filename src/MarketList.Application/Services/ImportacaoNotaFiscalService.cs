using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

public class ImportacaoNotaFiscalService : IImportacaoNotaFiscalService
{
    private readonly INotaFiscalCrawlerService _crawlerService;
    private readonly IEmpresaResolverService _empresaResolver;
    private readonly IListaDeComprasService _listaService;
    private readonly ILogger<ImportacaoNotaFiscalService> _logger;

    public ImportacaoNotaFiscalService(
        INotaFiscalCrawlerService crawlerService,
        IEmpresaResolverService empresaResolver,
        IListaDeComprasService listaService,
        ILogger<ImportacaoNotaFiscalService> logger)
    {
        _crawlerService = crawlerService;
        _empresaResolver = empresaResolver;
        _listaService = listaService;
        _logger = logger;
    }

    public async Task<ListaDeComprasDto> ImportarNotaPorUrlAsync(string urlNota, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando importação de nota fiscal por URL: {Url}", urlNota);

        var notaExtraida = await _crawlerService.BaixarEExtrairTextoAsync(urlNota, cancellationToken);

        var linhas = notaExtraida.Texto.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
        if (linhas.Length == 0 || string.IsNullOrWhiteSpace(linhas[0]))
        {
            throw new InvalidOperationException("Não foi possível identificar a empresa da nota fiscal.");
        }

        var nomeEmpresa = linhas[0].Trim();
        var textoItens = string.Join("\n", linhas.Skip(1)).Trim();

        if (string.IsNullOrWhiteSpace(textoItens))
        {
            throw new InvalidOperationException("Não foi possível extrair os itens da nota fiscal.");
        }

        var empresaId = await _empresaResolver.ResolverEmpresaIdPorNomeAsync(nomeEmpresa, cancellationToken);

        var nomeLista = $"Nota Fiscal - {nomeEmpresa} - {notaExtraida.DataEmissao:yyyy-MM-dd HH:mm:ss}";
        var createDto = new ListaDeComprasCreateDto(
            nomeLista,
            textoItens,
            TipoEntrada.NotaFiscal,
            empresaId,
            notaExtraida.DataEmissao
        );

        var lista = await _listaService.CreateAsync(createDto, cancellationToken);

        _logger.LogInformation(
            "Lista criada a partir de nota fiscal: {ListaId}, Empresa: {Empresa} (Id: {EmpresaId})",
            lista.Id,
            nomeEmpresa,
            empresaId);

        return lista;
    }
}
