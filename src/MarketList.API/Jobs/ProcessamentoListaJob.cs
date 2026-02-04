using MarketList.Application.Interfaces;

namespace MarketList.API.Jobs;

/// <summary>
/// Job Hangfire para processamento assíncrono de listas de compras.
/// Este job é executado quando uma nova lista é criada e realiza:
/// - Análise do texto original para identificar itens
/// - Detecção automática de categorias
/// - Criação/atualização de produtos
/// - Consulta de preços externos
/// - Cálculo de totais
/// </summary>
public class ProcessamentoListaJob
{
    private readonly IProcessamentoListaService _processamentoService;
    private readonly ILogger<ProcessamentoListaJob> _logger;

    public ProcessamentoListaJob(
        IProcessamentoListaService processamentoService,
        ILogger<ProcessamentoListaJob> logger)
    {
        _processamentoService = processamentoService;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma lista de compras de forma assíncrona
    /// </summary>
    /// <param name="listaId">ID da lista a ser processada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    public async Task ExecuteAsync(Guid listaId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Hangfire Job] Iniciando processamento da lista {ListaId}", listaId);
        
        try
        {
            await _processamentoService.ProcessarListaAsync(listaId, cancellationToken);
            _logger.LogInformation("[Hangfire Job] Lista {ListaId} processada com sucesso", listaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hangfire Job] Erro ao processar lista {ListaId}", listaId);
            throw; // Re-throw para que o Hangfire possa fazer retry
        }
    }
}
