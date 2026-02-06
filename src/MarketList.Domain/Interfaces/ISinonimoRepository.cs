using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório para sinônimos de produtos
/// </summary>
public interface ISinonimoRepository : IRepository<SinonimoProduto>
{
    /// <summary>
    /// Busca sinônimo pelo texto normalizado
    /// </summary>
    Task<SinonimoProduto?> FindByTextoNormalizadoAsync(string textoNormalizado, CancellationToken cancellationToken = default);
}
