using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório especializado para Categoria
/// </summary>
public interface ICategoriaRepository : IRepository<Categoria>
{
    /// <summary>
    /// Busca categoria pelo nome
    /// </summary>
    Task<Categoria?> FindByNomeAsync(string nome, CancellationToken cancellationToken = default);
}
