using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface ICategoriaService
{
    Task<PagedResultDto<CategoriaDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<CategoriaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoriaDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto, CancellationToken cancellationToken = default);
    Task<CategoriaDto?> UpdateAsync(Guid id, CategoriaUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
