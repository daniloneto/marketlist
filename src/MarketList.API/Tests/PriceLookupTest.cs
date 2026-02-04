using System.Net.Http;
using System.Threading.Tasks;
using MarketList.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace MarketList.API.Tests;

/// <summary>
/// Classe para testar manualmente o serviço de consulta de preços
/// Execute via endpoint ou console app temporário
/// </summary>
public class PriceLookupTest
{
    public static async Task TestPriceLookup()
    {
        // Configurar logger
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<PrecoDaHoraPriceLookupService>();
        
        // Criar HttpClient
        var httpClient = new HttpClient();
        
        // Criar serviço
        var service = new PrecoDaHoraPriceLookupService(httpClient, logger);
        
        // Testar produtos
        var produtos = new[] { "Arroz", "Feijão", "Leite", "Café", "Açúcar" };
        
        foreach (var produto in produtos)
        {
            Console.WriteLine($"\n=== Testando: {produto} ===");
            
            var resultado = await service.GetLatestPriceAsync(
                productNameOrGtin: produto,
                latitude: -12.9714,  // Salvador/BA
                longitude: -38.5014,
                hours: 24
            );
            
            if (resultado.Found)
            {
                Console.WriteLine($"✅ Preço encontrado: R$ {resultado.Price:N2}");
                Console.WriteLine($"   Loja: {resultado.StoreName ?? "N/A"}");
                Console.WriteLine($"   Data: {resultado.Date}");
            }
            else
            {
                Console.WriteLine("❌ Preço não encontrado");
            }
            
            // Delay entre testes
            await Task.Delay(3000);
        }
    }
}
