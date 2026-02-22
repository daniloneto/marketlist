using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IProductResolutionService
{
    Task<IReadOnlyList<ProductCatalogResolutionCandidateDto>> GetActiveCatalogSnapshotAsync(CancellationToken cancellationToken = default);
    Task<ProductResolutionResultDto> ResolveAsync(string rawProductName, IReadOnlyList<ProductCatalogResolutionCandidateDto>? catalogSnapshot = null, CancellationToken cancellationToken = default);
}
