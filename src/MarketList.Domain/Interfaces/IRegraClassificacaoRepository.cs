using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório para regras de classificação de categorias
/// </summary>
public interface IRegraClassificacaoRepository : IRepository<RegraClassificacaoCategoria>
{
    /// <summary>
    /// Retorna todas as regras ordenadas por prioridade (maior primeiro) e depois por contagem de usos
    /// </summary>
    Task<IEnumerable<RegraClassificacaoCategoria>> GetRegrasOrdenadasAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Incrementa o contador de usos de uma regra
    /// </summary>
    Task IncrementarContagemAsync(Guid regraId, CancellationToken cancellationToken = default);
}
