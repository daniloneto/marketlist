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
    private readonly IPrecoExternoApi _precoExternoApi;
    private readonly IRepository<HistoricoPreco> _historicoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AtualizacaoPrecosJob> _logger;

    public AtualizacaoPrecosJob(
        AppDbContext context,
        IPrecoExternoApi precoExternoApi,
        IRepository<HistoricoPreco> historicoRepository,
        IUnitOfWork unitOfWork,
        ILogger<AtualizacaoPrecosJob> logger)
    {
        _context = context;
        _precoExternoApi = precoExternoApi;
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
                var precoExterno = await _precoExternoApi.ConsultarPrecoAsync(produto.Nome, cancellationToken);
                
                if (precoExterno != null && precoExterno.Sucesso && precoExterno.Preco.HasValue)
                {
                    // Criar novo registro de histórico
                    var historico = new HistoricoPreco
                    {
                        Id = Guid.NewGuid(),
                        ProdutoId = produto.Id,
                        PrecoUnitario = precoExterno.Preco.Value,
                        DataConsulta = DateTime.UtcNow,
                        FontePreco = precoExterno.Fonte,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _historicoRepository.AddAsync(historico, cancellationToken);
                    atualizados++;

                    _logger.LogDebug("Preço atualizado para {Produto}: R$ {Preco:N2}", 
                        produto.Nome, precoExterno.Preco.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao atualizar preço do produto {Produto}", produto.Nome);
                erros++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Hangfire Job] Atualização de preços concluída. Produtos: {Total}, Atualizados: {Atualizados}, Erros: {Erros}",
            produtos.Count, atualizados, erros);
    }
}
