using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Jobs;

/// <summary>
/// Job Hangfire para atualização periódica de preços usando o IPriceLookupService.
/// Este exemplo demonstra como usar o Adapter para consultar preços do serviço "Preço da Hora".
/// </summary>
public class AtualizacaoPrecosComPriceLookupJob
{
    private readonly AppDbContext _context;
    private readonly IPriceLookupService _priceLookupService;
    private readonly IRepository<HistoricoPreco> _historicoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AtualizacaoPrecosComPriceLookupJob> _logger;

    // Coordenadas padrão - Salvador/BA (podem vir de configuração ou do usuário)
    private const double DefaultLatitude = -12.9714;
    private const double DefaultLongitude = -38.5014;

    public AtualizacaoPrecosComPriceLookupJob(
        AppDbContext context,
        IPriceLookupService priceLookupService,
        IRepository<HistoricoPreco> historicoRepository,
        IUnitOfWork unitOfWork,
        ILogger<AtualizacaoPrecosComPriceLookupJob> logger)
    {
        _context = context;
        _priceLookupService = priceLookupService;
        _historicoRepository = historicoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Atualiza os preços de todos os produtos usando o IPriceLookupService
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Hangfire Job] Iniciando atualização de preços com PriceLookupService");

        var produtos = await _context.Produtos.ToListAsync(cancellationToken);
        var atualizados = 0;
        var naoEncontrados = 0;
        var erros = 0;

        foreach (var produto in produtos)
        {
            try
            {
                // Usar o Adapter para consultar o preço
                // O Adapter abstrai completamente a implementação do serviço externo
                var resultado = await _priceLookupService.GetLatestPriceAsync(
                    productNameOrGtin: produto.Nome,
                    latitude: DefaultLatitude,
                    longitude: DefaultLongitude,
                    hours: 24
                );

                if (resultado.Found && resultado.Price.HasValue)
                {
                    // Criar novo registro de histórico
                    var historico = new HistoricoPreco
                    {
                        Id = Guid.NewGuid(),
                        ProdutoId = produto.Id,
                        PrecoUnitario = resultado.Price.Value,
                        DataConsulta = resultado.Date ?? DateTime.UtcNow,
                        FontePreco = $"Preço da Hora - {resultado.StoreName ?? "N/A"}",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _historicoRepository.AddAsync(historico, cancellationToken);
                    atualizados++;

                    _logger.LogDebug(
                        "Preço atualizado para {Produto}: R$ {Preco:N2} em {Loja}",
                        produto.Nome, resultado.Price.Value, resultado.StoreName ?? "N/A");
                }
                else
                {
                    naoEncontrados++;
                    _logger.LogDebug("Preço não encontrado para {Produto}", produto.Nome);
                }
            }
            catch (Exception ex)
            {
                // O Adapter já trata exceções, mas caso ocorra algum erro inesperado
                _logger.LogWarning(ex, "Erro ao atualizar preço do produto {Produto}", produto.Nome);
                erros++;
            }

            // Pequeno delay para não sobrecarregar o serviço externo
            await Task.Delay(500, cancellationToken);
        }

        // Salvar todas as alterações de uma vez
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Hangfire Job] Atualização de preços concluída. " +
            "Total: {Total}, Atualizados: {Atualizados}, Não encontrados: {NaoEncontrados}, Erros: {Erros}",
            produtos.Count, atualizados, naoEncontrados, erros);
    }

    /// <summary>
    /// Atualiza o preço de um produto específico
    /// Útil para atualizações sob demanda
    /// </summary>
    public async Task<bool> AtualizarProdutoEspecificoAsync(
        Guid produtoId,
        double? latitude = null,
        double? longitude = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var produto = await _context.Produtos.FindAsync(new object[] { produtoId }, cancellationToken);
            if (produto == null)
            {
                _logger.LogWarning("Produto {ProdutoId} não encontrado", produtoId);
                return false;
            }

            var resultado = await _priceLookupService.GetLatestPriceAsync(
                productNameOrGtin: produto.Nome,
                latitude: latitude ?? DefaultLatitude,
                longitude: longitude ?? DefaultLongitude,
                hours: 24
            );

            if (resultado.Found && resultado.Price.HasValue)
            {
                var historico = new HistoricoPreco
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produto.Id,
                    PrecoUnitario = resultado.Price.Value,
                    DataConsulta = resultado.Date ?? DateTime.UtcNow,
                    FontePreco = $"Preço da Hora - {resultado.StoreName ?? "N/A"}",
                    CreatedAt = DateTime.UtcNow
                };

                await _historicoRepository.AddAsync(historico, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Preço atualizado para {Produto}: R$ {Preco:N2}",
                    produto.Nome, resultado.Price.Value);

                return true;
            }

            _logger.LogWarning("Preço não encontrado para {Produto}", produto.Nome);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar preço do produto {ProdutoId}", produtoId);
            return false;
        }
    }
}
