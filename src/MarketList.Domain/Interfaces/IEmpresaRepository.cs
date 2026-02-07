using MarketList.Domain.Entities;

namespace MarketList.Domain.Interfaces;

/// <summary>
/// Repositório especializado para Empresa (supermercados)
/// </summary>
public interface IEmpresaRepository : IRepository<Empresa>
{
    /// <summary>
    /// Busca empresa pelo nome
    /// </summary>
    Task<Empresa?> FindByNomeAsync(string nome, CancellationToken cancellationToken = default);
}
