using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Infrastructure.Services;

/// <summary>
/// Implementação mock da API de preços externos
/// Em produção, substituir por chamada HTTP real
/// </summary>
public class PrecoExternoApiMock : IPrecoExternoApi
{
    private readonly ILogger<PrecoExternoApiMock> _logger;
    
    // Preço fixo para todos os produtos
    private const decimal PrecoFixo = 1.00m;

    public PrecoExternoApiMock(ILogger<PrecoExternoApiMock> logger)
    {
        _logger = logger;
    }

    public async Task<PrecoExternoDto> ConsultarPrecoAsync(string nomeProduto, CancellationToken cancellationToken = default)
    {
        // Simular delay de rede
        await Task.Delay(100, cancellationToken);

        _logger.LogDebug("[MOCK] Consultando preço para {Produto} - Retornando R$ 1,00", nomeProduto);

        return new PrecoExternoDto(
            NomeProduto: nomeProduto,
            Preco: PrecoFixo,
            Fonte: "API Mock (R$ 1,00 fixo)",
            Sucesso: true,
            Erro: null
        );
    }
}
