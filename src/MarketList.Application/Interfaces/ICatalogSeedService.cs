namespace MarketList.Application.Interfaces;

public interface ICatalogSeedService
{
    Task SeedFromCsvAsync(CancellationToken cancellationToken = default);
}
