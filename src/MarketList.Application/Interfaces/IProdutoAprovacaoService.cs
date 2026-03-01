using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço para aprovação e curadoria de produtos pendentes
/// </summary>
public interface IProdutoAprovacaoService
{
    /// <summary>
    /// Lista todos os produtos pendentes de revisão (nome ou categoria)
    /// </summary>
    Task<PagedResultDto<ProdutoPendenteDto>> ListarPendentesRevisaoAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aprova um produto com correções (nome e/ou categoria)
    /// </summary>
    Task AprovarComCorrecaoAsync(Guid produtoId, ProdutoAprovacaoDto aprovacao, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vincula um produto provisório a um produto existente (merge)
    /// </summary>
    Task VincularProdutosAsync(Guid produtoOrigemId, Guid produtoDestinoId, CancellationToken cancellationToken = default);
}
