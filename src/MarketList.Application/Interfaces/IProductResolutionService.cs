using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IProductResolutionService
{
    Task<ProductResolutionResultDto> ResolveAsync(string rawProductName, CancellationToken cancellationToken = default);
}
