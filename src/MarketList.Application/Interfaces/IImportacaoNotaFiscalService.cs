using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IImportacaoNotaFiscalService
{
    Task<ListaDeComprasDto> ImportarNotaPorUrlAsync(string urlNota, CancellationToken cancellationToken = default);
}
