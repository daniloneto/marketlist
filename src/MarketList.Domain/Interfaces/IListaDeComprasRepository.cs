using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório especializado para ListaDeCompras
/// </summary>
public interface IListaDeComprasRepository : IRepository<ListaDeCompras>
{
    /// <summary>
    /// Obtém as listas de compras de um usuário
    /// </summary>
    Task<IEnumerable<ListaDeCompras>> GetByUsuarioIdAsync(string usuarioId, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém lista com todos osItens
    /// </summary>
    Task<ListaDeCompras?> GetWithItensAsync(Guid id, CancellationToken cancellationToken = default);
}
