using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório especializado para Produto com operações customizadas de busca e matching
/// </summary>
public interface IProdutoRepository : IRepository<Produto>
{
    /// <summary>
    /// Busca produto pelo código da loja
    /// </summary>
    Task<Produto?> FindByCodigoLojaAsync(string codigoLoja, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca produto pelo nome normalizado (match exato)
    /// </summary>
    Task<Produto?> FindByNomeNormalizadoAsync(string nomeNormalizado, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca produtos com nome similar usando fuzzy matching
    /// </summary>
    /// <param name="nome">Nome a ser comparado</param>
    /// <param name="maxResults">Número máximo de resultados</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Lista ordenada por similaridade (mais similar primeiro)</returns>
    Task<IEnumerable<Produto>> FindSimilarByNameAsync(string nome, int maxResults = 5, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retorna produtos que precisam de revisão de nome
    /// </summary>
    Task<IEnumerable<Produto>> GetPendingReviewAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retorna produtos que precisam de revisão de categoria
    /// </summary>
    Task<IEnumerable<Produto>> GetPendingCategoryReviewAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Migra todos os registros relacionados (HistoricoPrecos, ItensLista) de um produto para outro
    /// Usado quando produtos duplicados são vinculados/mesclados
    /// </summary>
    Task MigrateProdutoAsync(Guid fromId, Guid toId, CancellationToken cancellationToken = default);
}
