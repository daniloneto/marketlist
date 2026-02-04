using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Interface para a API externa de consulta de preços (mockável)
/// </summary>
public interface IPrecoExternoApi
{
    Task<PrecoExternoDto> ConsultarPrecoAsync(string nomeProduto, CancellationToken cancellationToken = default);
}
