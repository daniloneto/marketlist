using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

public class ProdutoAprovacaoService : IProdutoAprovacaoService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly ISinonimoRepository _sinonimoRepository;
    private readonly ICategoriaClassificadorService _classificadorService;
    private readonly ITextoNormalizacaoService _normalizacaoService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProdutoAprovacaoService> _logger;

    public ProdutoAprovacaoService(
        IProdutoRepository produtoRepository,
        ISinonimoRepository sinonimoRepository,
        ICategoriaClassificadorService classificadorService,
        ITextoNormalizacaoService normalizacaoService,
        IUnitOfWork unitOfWork,
        ILogger<ProdutoAprovacaoService> logger)
    {
        _produtoRepository = produtoRepository;
        _sinonimoRepository = sinonimoRepository;
        _classificadorService = classificadorService;
        _normalizacaoService = normalizacaoService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProdutoPendenteDto>> ListarPendentesRevisaoAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        // Buscar produtos que precisam de revisão (nome OU categoria)
        var produtosPendentesNome = await _produtoRepository.GetPendingReviewAsync(cancellationToken);
        var produtosPendentesCategoria = await _produtoRepository.GetPendingCategoryReviewAsync(cancellationToken);

        // Unir as duas listas (alguns produtos podem estar nas duas)
        var todosPendentes = produtosPendentesNome
            .Union(produtosPendentesCategoria)
            .DistinctBy(p => p.Id)
            .OrderByDescending(p => p.CreatedAt);

        var resultado = new List<ProdutoPendenteDto>();

        foreach (var produto in todosPendentes)
        {
            // Buscar produtos similares para sugerir vinculação
            var similares = await _produtoRepository.FindSimilarByNameAsync(produto.Nome, 5, cancellationToken);
            var similaresList = similares
                .Where(s => s.Id != produto.Id) // Excluir o próprio produto
                .Select(s => new ProdutoResumoDto(s.Id, s.Nome, s.Unidade))
                .ToList();

            resultado.Add(new ProdutoPendenteDto(
                produto.Id,
                produto.Nome,
                produto.Descricao,
                produto.Unidade,
                produto.CodigoLoja,
                produto.CategoriaId,
                produto.Categoria.Nome,
                produto.PrecisaRevisao,
                produto.CategoriaPrecisaRevisao,
                produto.CreatedAt,
                similaresList
            ));
        }

        var totalCount = resultado.Count;
        var items = resultado
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<ProdutoPendenteDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task AprovarComCorrecaoAsync(Guid produtoId, ProdutoAprovacaoDto aprovacao, CancellationToken cancellationToken = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(produtoId, cancellationToken);
        if (produto == null)
        {
            throw new InvalidOperationException($"Produto {produtoId} não encontrado");
        }

        // 1. Corrigir nome se fornecido
        if (!string.IsNullOrWhiteSpace(aprovacao.NomeCorrigido) && aprovacao.NomeCorrigido != produto.Nome)
        {
            var nomeAntigo = produto.Nome;
            var nomeAntigoNormalizado = produto.NomeNormalizado ?? _normalizacaoService.Normalizar(nomeAntigo);

            // Criar sinônimo para o nome antigo
            var sinonimo = new SinonimoProduto
            {
                Id = Guid.NewGuid(),
                TextoOriginal = nomeAntigo,
                TextoNormalizado = nomeAntigoNormalizado,
                ProdutoId = produtoId,
                FonteOrigem = "CorrecaoManual",
                CreatedAt = DateTime.UtcNow
            };

            await _sinonimoRepository.AddAsync(sinonimo, cancellationToken);
            
            _logger.LogInformation("Sinônimo criado: '{TextoOriginal}' -> '{TextoNormalizado}' para produto {ProdutoId}", 
                nomeAntigo, nomeAntigoNormalizado, produtoId);

            // Atualizar produto com nome correto
            produto.Nome = aprovacao.NomeCorrigido;
            produto.NomeNormalizado = _normalizacaoService.Normalizar(aprovacao.NomeCorrigido);
            
            _logger.LogInformation("Nome do produto {ProdutoId} corrigido: '{NomeAntigo}' -> '{NomeNovo}'", 
                produtoId, nomeAntigo, aprovacao.NomeCorrigido);
        }
        else if (!string.IsNullOrWhiteSpace(aprovacao.NomeCorrigido))
        {
            _logger.LogWarning("Sinônimo NÃO criado para produto {ProdutoId}. Nome atual: '{NomeAtual}', Nome corrigido: '{NomeCorrigido}'", 
                produtoId, produto.Nome, aprovacao.NomeCorrigido);
        }

        // 2. Corrigir categoria se fornecida
        if (aprovacao.CategoriaIdCorrigida.HasValue && aprovacao.CategoriaIdCorrigida.Value != produto.CategoriaId)
        {
            produto.CategoriaId = aprovacao.CategoriaIdCorrigida.Value;
            
            // Aprender a nova classificação para produtos futuros
            await _classificadorService.AprenderClassificacaoAsync(produtoId, aprovacao.CategoriaIdCorrigida.Value, cancellationToken);
            
            produto.CategoriaPrecisaRevisao = false;
            
            _logger.LogInformation("Categoria do produto {ProdutoId} corrigida para {CategoriaId}", 
                produtoId, aprovacao.CategoriaIdCorrigida.Value);
        }

        // 3. Marcar como aprovado (não precisa mais de revisão)
        produto.PrecisaRevisao = false;
        
        // Se só tinha pendência de categoria e foi resolvida, remove também
        if (!aprovacao.CategoriaIdCorrigida.HasValue && produto.CategoriaPrecisaRevisao)
        {
            // Mantém a pendência de categoria se não foi corrigida
        }
        else if (aprovacao.CategoriaIdCorrigida.HasValue)
        {
            produto.CategoriaPrecisaRevisao = false;
        }

        await _produtoRepository.UpdateAsync(produto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Produto {ProdutoId} aprovado com sucesso", produtoId);
    }

    public async Task VincularProdutosAsync(Guid produtoOrigemId, Guid produtoDestinoId, CancellationToken cancellationToken = default)
    {
        var produtoOrigem = await _produtoRepository.GetByIdAsync(produtoOrigemId, cancellationToken);
        var produtoDestino = await _produtoRepository.GetByIdAsync(produtoDestinoId, cancellationToken);

        if (produtoOrigem == null)
            throw new InvalidOperationException($"Produto origem {produtoOrigemId} não encontrado");

        if (produtoDestino == null)
            throw new InvalidOperationException($"Produto destino {produtoDestinoId} não encontrado");

        // 1. Criar sinônimo apontando para o produto correto
        var nomeOrigemNormalizado = produtoOrigem.NomeNormalizado ?? _normalizacaoService.Normalizar(produtoOrigem.Nome);
        
        var sinonimo = new SinonimoProduto
        {
            Id = Guid.NewGuid(),
            TextoOriginal = produtoOrigem.Nome,
            TextoNormalizado = nomeOrigemNormalizado,
            ProdutoId = produtoDestinoId, // Aponta para o produto correto
            FonteOrigem = "Vinculacao",
            CreatedAt = DateTime.UtcNow
        };

        await _sinonimoRepository.AddAsync(sinonimo, cancellationToken);

        // 2. Migrar registros relacionados (histórico de preços, itens de lista)
        await _produtoRepository.MigrateProdutoAsync(produtoOrigemId, produtoDestinoId, cancellationToken);

        // 3. Remover produto origem (cascade delete cuidará dos sinônimos dele)
        await _produtoRepository.DeleteAsync(produtoOrigem, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Produtos vinculados: {OrigemId} ({OrigemNome}) -> {DestinoId} ({DestinoNome})",
            produtoOrigemId, produtoOrigem.Nome, produtoDestinoId, produtoDestino.Nome);
    }
}
