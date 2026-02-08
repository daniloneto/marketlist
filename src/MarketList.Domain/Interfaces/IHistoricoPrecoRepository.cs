using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório especializado para HistoricoPreco
/// </summary>
public interface IHistoricoPrecoRepository : IRepository<HistoricoPreco>
{
    /// <summary>
    /// Obtém o histórico de preços de um produto
    /// </summary>
    Task<IEnumerable<HistoricoPreco>> GetByProdutoIdAsync(Guid produtoId, int days = 90, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o preço mais recente de um produto
    /// </summary>
    Task<HistoricoPreco?> GetLatestByProdutoIdAsync(Guid produtoId, CancellationToken cancellationToken = default);
}
