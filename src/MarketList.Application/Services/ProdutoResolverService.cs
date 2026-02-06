using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

public class ProdutoResolverService : IProdutoResolverService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly ISinonimoRepository _sinonimoRepository;
    private readonly ITextoNormalizacaoService _normalizacaoService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProdutoResolverService> _logger;

    public ProdutoResolverService(
        IProdutoRepository produtoRepository,
        ISinonimoRepository sinonimoRepository,
        ITextoNormalizacaoService normalizacaoService,
        IUnitOfWork unitOfWork,
        ILogger<ProdutoResolverService> logger)
    {
        _produtoRepository = produtoRepository;
        _sinonimoRepository = sinonimoRepository;
        _normalizacaoService = normalizacaoService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProdutoResolucaoResultado> ResolverProdutoAsync(
        string nomeOriginal,
        string? codigoLoja,
        Guid categoriaId,
        CancellationToken cancellationToken = default)
    {
        // 1. Tentar por código da loja (mais confiável)
        if (!string.IsNullOrWhiteSpace(codigoLoja))
        {
            var produtoPorCodigo = await _produtoRepository.FindByCodigoLojaAsync(codigoLoja, cancellationToken);
            if (produtoPorCodigo != null)
            {
                _logger.LogInformation("Produto encontrado por CodigoLoja: {CodigoLoja} -> {ProdutoId}", 
                    codigoLoja, produtoPorCodigo.Id);
                
                return new ProdutoResolucaoResultado
                {
                    Produto = produtoPorCodigo,
                    FoiCriado = false,
                    Origem = OrigemResolucao.CodigoLoja
                };
            }
        }

        // 2. Normalizar o nome
        var nomeNormalizado = _normalizacaoService.Normalizar(nomeOriginal);

        // 3. Tentar por sinônimo
        var sinonimo = await _sinonimoRepository.FindByTextoNormalizadoAsync(nomeNormalizado, cancellationToken);
        if (sinonimo != null)
        {
            _logger.LogInformation("Produto encontrado por sinônimo: '{Texto}' -> {ProdutoId}", 
                nomeOriginal, sinonimo.ProdutoId);
            
            return new ProdutoResolucaoResultado
            {
                Produto = sinonimo.Produto,
                FoiCriado = false,
                Origem = OrigemResolucao.Sinonimo
            };
        }

        // 4. Tentar por nome normalizado exato
        var produtoPorNome = await _produtoRepository.FindByNomeNormalizadoAsync(nomeNormalizado, cancellationToken);
        if (produtoPorNome != null)
        {
            _logger.LogInformation("Produto encontrado por nome normalizado: '{Nome}' -> {ProdutoId}", 
                nomeOriginal, produtoPorNome.Id);
            
            // Se o produto existe mas não tinha CodigoLoja, atualizar
            if (!string.IsNullOrWhiteSpace(codigoLoja) && string.IsNullOrWhiteSpace(produtoPorNome.CodigoLoja))
            {
                produtoPorNome.CodigoLoja = codigoLoja;
                await _produtoRepository.UpdateAsync(produtoPorNome, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("CodigoLoja atualizado para produto {ProdutoId}: {CodigoLoja}", 
                    produtoPorNome.Id, codigoLoja);
            }
            
            return new ProdutoResolucaoResultado
            {
                Produto = produtoPorNome,
                FoiCriado = false,
                Origem = OrigemResolucao.NomeExato
            };
        }

        // 5. Produto não encontrado - criar novo (provisório, precisará de revisão)
        var novoProduto = new Produto
        {
            Id = Guid.NewGuid(),
            Nome = nomeOriginal,
            NomeNormalizado = nomeNormalizado,
            CodigoLoja = codigoLoja,
            CategoriaId = categoriaId,
            PrecisaRevisao = true, // Marca para revisão
            CreatedAt = DateTime.UtcNow
        };

        await _produtoRepository.AddAsync(novoProduto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Produto CRIADO (precisa revisão): '{Nome}' (Normalizado: '{Normalizado}', Código: {Codigo})", 
            nomeOriginal, nomeNormalizado, codigoLoja ?? "N/A");

        return new ProdutoResolucaoResultado
        {
            Produto = novoProduto,
            FoiCriado = true,
            Origem = OrigemResolucao.Criado
        };
    }
}
