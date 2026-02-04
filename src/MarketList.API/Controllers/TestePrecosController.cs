using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestePrecosController : ControllerBase
{
    private readonly IPriceLookupService _priceLookupService;
    private readonly ILogger<TestePrecosController> _logger;

    public TestePrecosController(
        IPriceLookupService priceLookupService,
        ILogger<TestePrecosController> logger)
    {
        _priceLookupService = priceLookupService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de teste para consultar preço de um produto específico
    /// GET /api/testeprecos/arroz
    /// </summary>
    [HttpGet("{produto}")]
    public async Task<IActionResult> ConsultarPreco(string produto)
    {
        _logger.LogInformation("Teste de consulta de preço para: {Produto}", produto);

        var resultado = await _priceLookupService.GetLatestPriceAsync(
            productNameOrGtin: produto,
            latitude: -12.9714,  // Salvador/BA
            longitude: -38.5014,
            hours: 24
        );

        if (resultado.Found)
        {
            return Ok(new
            {
                sucesso = true,
                produto = produto,
                preco = resultado.Price,
                loja = resultado.StoreName,
                data = resultado.Date,
                mensagem = $"Preço encontrado: R$ {resultado.Price:N2}"
            });
        }

        return Ok(new
        {
            sucesso = false,
            produto = produto,
            mensagem = "Preço não encontrado"
        });
    }

    /// <summary>
    /// Endpoint para testar múltiplos produtos
    /// POST /api/testeprecos/batch
    /// Body: ["arroz", "feijao", "leite"]
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> ConsultarPrecosBatch([FromBody] string[] produtos)
    {
        _logger.LogInformation("Teste batch de {Count} produtos", produtos.Length);

        var resultados = new List<object>();

        foreach (var produto in produtos)
        {
            var resultado = await _priceLookupService.GetLatestPriceAsync(
                productNameOrGtin: produto,
                latitude: -12.9714,
                longitude: -38.5014,
                hours: 24
            );

            resultados.Add(new
            {
                produto = produto,
                encontrado = resultado.Found,
                preco = resultado.Price,
                loja = resultado.StoreName,
                data = resultado.Date
            });

            // Delay entre requisições
            await Task.Delay(2000);
        }

        return Ok(new
        {
            total = produtos.Length,
            encontrados = resultados.Count(r => ((dynamic)r).encontrado),
            resultados = resultados
        });
    }
}
