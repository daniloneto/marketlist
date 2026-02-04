using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Jobs;

/// <summary>
/// Job Hangfire para atualização periódica de preços de produtos.
/// Este job é executado em um schedule recorrente e atualiza os preços
/// de todos os produtos consultando a API externa.
/// </summary>
public class AtualizacaoPrecosJob
{
    private readonly AppDbContext _context;
    private readonly IPriceLookupService _priceLookupService;
    private readonly IRepository<HistoricoPreco> _historicoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AtualizacaoPrecosJob> _logger;

    // Coordenadas padrão - Salvador/BA (podem vir de configuração)
    private const double DefaultLatitude = -12.9714;
    private const double DefaultLongitude = -38.5014;

    public AtualizacaoPrecosJob(
        AppDbContext context,
        IPriceLookupService priceLookupService,
        IRepository<HistoricoPreco> historicoRepository,
        IUnitOfWork unitOfWork,
        ILogger<AtualizacaoPrecosJob> logger)
    {
        _context = context;
        _priceLookupService = priceLookupService;
        _historicoRepository = historicoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Atualiza os preços de todos os produtos
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Hangfire Job] Iniciando atualização de preços");

        var produtos = await _context.Produtos.ToListAsync(cancellationToken);
        var atualizados = 0;
        var erros = 0;

        foreach (var produto in produtos)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao atualizar preço do produto {Produto}", produto.Nome);
                erros++;
            }

            // Pequeno delay para não sobrecarregar o serviço externo
            await Task.Delay(500, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Hangfire Job] Atualização de preços concluída. Produtos: {Total}, Atualizados: {Atualizados}, Erros: {Erros}",
            produtos.Count, atualizados, erros);
    }
}
