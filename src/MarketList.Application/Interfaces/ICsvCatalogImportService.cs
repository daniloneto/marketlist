namespace MarketList.Application.Interfaces;

public interface ICsvCatalogImportService
{
    Task ImportAsync(CancellationToken cancellationToken = default);
}
