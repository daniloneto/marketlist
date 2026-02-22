using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface ICatalogTaxonomyService
{
    Task<IReadOnlyList<CatalogCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CatalogCategoryDto> CreateCategoryAsync(CatalogCategoryCreateDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogSubcategoryDto>> GetSubcategoriesAsync(CancellationToken cancellationToken = default);
    Task<CatalogSubcategoryDto> CreateSubcategoryAsync(CatalogSubcategoryCreateDto dto, CancellationToken cancellationToken = default);
}
