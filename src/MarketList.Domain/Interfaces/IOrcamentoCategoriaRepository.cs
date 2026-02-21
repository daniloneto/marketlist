using MarketList.Domain.Entities;
using MarketList.Domain.Enums;

namespace MarketList.Domain.Interfaces;

public interface IOrcamentoCategoriaRepository : IRepository<OrcamentoCategoria>
{
    Task<OrcamentoCategoria?> GetByCategoriaAndPeriodoAsync(
        Guid usuarioId,
        Guid categoriaId,
        PeriodoOrcamentoTipo periodoTipo,
        string periodoReferencia,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OrcamentoCategoria>> ListByPeriodoAsync(
        Guid usuarioId,
        PeriodoOrcamentoTipo periodoTipo,
        string periodoReferencia,
        CancellationToken cancellationToken = default);

    Task<OrcamentoCategoria> UpsertAsync(
        OrcamentoCategoria entity,
        CancellationToken cancellationToken = default);
}
