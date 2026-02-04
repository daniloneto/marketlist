namespace MarketList.Application.Interfaces;

/// <summary>
/// Interface para o serviço de processamento batch das listas
/// </summary>
public interface IProcessamentoListaService
{
    Task ProcessarListaAsync(Guid listaId, CancellationToken cancellationToken = default);
}
