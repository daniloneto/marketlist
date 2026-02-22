using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductCatalogDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCatalogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductCatalogDto> CreateAsync(ProductCatalogCreateDto dto, CancellationToken cancellationToken = default);
    Task<ProductCatalogDto?> UpdateAsync(Guid id, ProductCatalogUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
