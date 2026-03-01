using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IProdutoService
{
    Task<PagedResultDto<ProdutoDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ProdutoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProdutoDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProdutoDto>> GetByCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<ProdutoDto> CreateAsync(ProdutoCreateDto dto, CancellationToken cancellationToken = default);
    Task<ProdutoDto?> UpdateAsync(Guid id, ProdutoUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> GerarListaSimplesAsync(CancellationToken cancellationToken = default);
}
