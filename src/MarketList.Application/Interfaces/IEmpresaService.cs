using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IEmpresaService
{
    Task<IEnumerable<EmpresaDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmpresaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmpresaDto> CreateAsync(EmpresaCreateDto dto, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> UpdateAsync(Guid id, EmpresaUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
