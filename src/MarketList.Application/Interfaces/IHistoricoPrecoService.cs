using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IHistoricoPrecoService
{
    Task<PagedResultDto<HistoricoPrecoDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<HistoricoPrecoDto>> GetByProdutoAsync(Guid produtoId, CancellationToken cancellationToken = default);
    Task<HistoricoPrecoDto?> GetUltimoPrecoAsync(Guid produtoId, CancellationToken cancellationToken = default);
    Task<HistoricoPrecoDto> CreateAsync(HistoricoPrecoCreateDto dto, CancellationToken cancellationToken = default);
}
