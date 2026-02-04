using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IProdutoService
{
    Task<IEnumerable<ProdutoDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProdutoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProdutoDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProdutoDto>> GetByCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<ProdutoDto> CreateAsync(ProdutoCreateDto dto, CancellationToken cancellationToken = default);
    Task<ProdutoDto?> UpdateAsync(Guid id, ProdutoUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
