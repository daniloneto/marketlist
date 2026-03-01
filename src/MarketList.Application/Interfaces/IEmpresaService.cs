using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IEmpresaService
{
    Task<PagedResultDto<EmpresaDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmpresaDto> CreateAsync(EmpresaCreateDto dto, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> UpdateAsync(Guid id, EmpresaUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
