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
    private static readonly Random _random = new();

    // Preços mockados para alguns produtos
    private static readonly Dictionary<string, decimal> PrecosMock = new(StringComparer.OrdinalIgnoreCase)
    {
        ["leite"] = 5.99m,
        ["pão"] = 8.50m,
        ["arroz"] = 25.90m,
        ["feijão"] = 8.99m,
        ["macarrão"] = 4.50m,
        ["carne"] = 45.90m,
        ["frango"] = 18.90m,
        ["ovo"] = 15.90m,
        ["ovos"] = 15.90m,
        ["queijo"] = 35.90m,
        ["manteiga"] = 12.90m,
        ["café"] = 18.90m,
        ["açúcar"] = 5.90m,
        ["sal"] = 2.50m,
        ["óleo"] = 9.90m,
        ["azeite"] = 35.00m,
        ["tomate"] = 8.99m,
        ["cebola"] = 6.99m,
        ["alho"] = 25.00m,
        ["batata"] = 7.50m,
        ["banana"] = 6.99m,
        ["maçã"] = 12.90m,
        ["laranja"] = 4.99m,
        ["alface"] = 3.50m,
        ["detergente"] = 2.99m,
        ["sabão"] = 8.90m,
        ["papel higiênico"] = 22.90m,
        ["shampoo"] = 15.90m,
        ["cerveja"] = 4.50m,
        ["refrigerante"] = 7.99m,
        ["água"] = 2.50m,
        ["suco"] = 8.90m,
        ["iogurte"] = 6.50m,
        ["presunto"] = 45.90m,
        ["mortadela"] = 18.90m
    };

    public PrecoExternoApiMock(ILogger<PrecoExternoApiMock> logger)
    {
        _logger = logger;
    }

    public async Task<PrecoExternoDto> ConsultarPrecoAsync(string nomeProduto, CancellationToken cancellationToken = default)
    {
        // Simula latência de API externa
        await Task.Delay(_random.Next(100, 500), cancellationToken);

        _logger.LogInformation("Consultando preço para: {Produto}", nomeProduto);

        // Busca preço mockado
        foreach (var (key, preco) in PrecosMock)
        {
            if (nomeProduto.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                // Adiciona variação aleatória de +-10%
                var variacao = (decimal)(_random.NextDouble() * 0.2 - 0.1);
                var precoFinal = Math.Round(preco * (1 + variacao), 2);

                _logger.LogInformation("Preço encontrado para {Produto}: R$ {Preco}", nomeProduto, precoFinal);

                return new PrecoExternoDto(
                    NomeProduto: nomeProduto,
                    Preco: precoFinal,
                    Fonte: "API Mock - Supermercados Brasil",
                    Sucesso: true,
                    Erro: null
                );
            }
        }

        // Produto não encontrado - retorna preço aleatório entre R$ 5 e R$ 50
        var precoAleatorio = Math.Round((decimal)(_random.NextDouble() * 45 + 5), 2);

        _logger.LogInformation("Preço não catalogado para {Produto}, usando estimativa: R$ {Preco}", 
            nomeProduto, precoAleatorio);

        return new PrecoExternoDto(
            NomeProduto: nomeProduto,
            Preco: precoAleatorio,
            Fonte: "API Mock - Estimativa",
            Sucesso: true,
            Erro: null
        );
    }
}
