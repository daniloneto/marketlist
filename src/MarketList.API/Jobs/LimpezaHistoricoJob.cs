using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Jobs;

/// <summary>
/// Job Hangfire para limpeza de histórico de preços antigos.
/// Mantém apenas os registros dos últimos 120 dias para cada produto.
/// </summary>
public class LimpezaHistoricoJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<LimpezaHistoricoJob> _logger;

    public LimpezaHistoricoJob(
        AppDbContext context,
        ILogger<LimpezaHistoricoJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Remove registros de histórico com mais de 120 dias, mantendo
    /// pelo menos o registro mais recente de cada produto.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Hangfire Job] Iniciando limpeza de histórico de preços");

        var dataLimite = DateTime.UtcNow.AddDays(-120);

        // Buscar IDs dos registros a manter (mais recente de cada produto)
        var idsRecentes = await _context.HistoricoPrecos
            .GroupBy(h => h.ProdutoId)
            .Select(g => g.OrderByDescending(h => h.DataConsulta).First().Id)
            .ToListAsync(cancellationToken);

        // Remover históricos antigos, exceto o mais recente de cada produto
        var registrosRemovidos = await _context.HistoricoPrecos
            .Where(h => h.DataConsulta < dataLimite && !idsRecentes.Contains(h.Id))
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "[Hangfire Job] Limpeza de histórico concluída. {Count} registros removidos",
            registrosRemovidos);
    }
}
