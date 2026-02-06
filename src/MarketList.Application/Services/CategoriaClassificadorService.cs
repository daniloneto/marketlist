using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

public class CategoriaClassificadorService : ICategoriaClassificadorService
{
    private readonly IRegraClassificacaoRepository _regraRepository;
    private readonly IRepository<Categoria> _categoriaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ITextoNormalizacaoService _normalizacaoService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoriaClassificadorService> _logger;

    // Cache para categoria "Outros"
    private Guid? _categoriaOutrosId;

    public CategoriaClassificadorService(
        IRegraClassificacaoRepository regraRepository,
        IRepository<Categoria> categoriaRepository,
        IProdutoRepository produtoRepository,
        ITextoNormalizacaoService normalizacaoService,
        IUnitOfWork unitOfWork,
        ILogger<CategoriaClassificadorService> logger)
    {
        _regraRepository = regraRepository;
        _categoriaRepository = categoriaRepository;
        _produtoRepository = produtoRepository;
        _normalizacaoService = normalizacaoService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CategoriaClassificacaoResultado> ClassificarAsync(
        string nomeProduto,
        CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = _normalizacaoService.Normalizar(nomeProduto);

        // Buscar todas as regras ordenadas por prioridade
        var regras = await _regraRepository.GetRegrasOrdenadasAsync(cancellationToken);

        // Procurar a primeira regra que casa com o nome do produto
        foreach (var regra in regras)
        {
            if (nomeNormalizado.Contains(regra.TermoNormalizado))
            {
                // Incrementar contador de uso da regra
                await _regraRepository.IncrementarContagemAsync(regra.Id, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Produto '{Nome}' classificado como '{Categoria}' via regra '{Termo}'",
                    nomeProduto, regra.Categoria.Nome, regra.TermoNormalizado);

                return new CategoriaClassificacaoResultado
                {
                    CategoriaId = regra.CategoriaId,
                    Confianca = Confianca.Alta,
                    RegraAplicadaId = regra.Id
                };
            }
        }

        // Nenhuma regra encontrada - usar categoria "Outros"
        var categoriaOutrosId = await ObterCategoriaOutrosIdAsync(cancellationToken);

        _logger.LogWarning("Produto '{Nome}' caiu em categoria 'Outros' (nenhuma regra aplicável)", nomeProduto);

        return new CategoriaClassificacaoResultado
        {
            CategoriaId = categoriaOutrosId,
            Confianca = Confianca.Baixa,
            RegraAplicadaId = null
        };
    }

    public async Task AprenderClassificacaoAsync(
        Guid produtoId,
        Guid categoriaId,
        CancellationToken cancellationToken = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(produtoId, cancellationToken);
        if (produto == null)
        {
            _logger.LogWarning("Produto {ProdutoId} não encontrado para aprendizado", produtoId);
            return;
        }

        var nomeNormalizado = produto.NomeNormalizado ?? _normalizacaoService.Normalizar(produto.Nome);

        // Extrair termos significativos do nome (palavras com 3+ caracteres)
        var termos = nomeNormalizado
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3)
            .ToList();

        if (!termos.Any())
        {
            _logger.LogWarning("Nenhum termo significativo encontrado em '{Nome}' para criar regra", produto.Nome);
            return;
        }

        // Criar regras para cada termo (termos mais longos têm prioridade maior)
        var regrasExistentes = await _regraRepository.GetRegrasOrdenadasAsync(cancellationToken);
        var termosExistentes = regrasExistentes.Select(r => r.TermoNormalizado).ToHashSet();

        foreach (var termo in termos)
        {
            if (termosExistentes.Contains(termo))
            {
                _logger.LogInformation("Regra para termo '{Termo}' já existe, pulando", termo);
                continue;
            }

            var novaRegra = new RegraClassificacaoCategoria
            {
                Id = Guid.NewGuid(),
                TermoNormalizado = termo,
                CategoriaId = categoriaId,
                Prioridade = termo.Length, // Termos mais longos = maior prioridade
                ContagemUsos = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _regraRepository.AddAsync(novaRegra, cancellationToken);
            _logger.LogInformation("Nova regra criada: '{Termo}' -> Categoria {CategoriaId} (Prioridade: {Prioridade})",
                termo, categoriaId, novaRegra.Prioridade);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid> ObterCategoriaOutrosIdAsync(CancellationToken cancellationToken)
    {
        if (_categoriaOutrosId.HasValue)
            return _categoriaOutrosId.Value;

        var categorias = await _categoriaRepository.GetAllAsync(cancellationToken);
        var categoriaOutros = categorias.FirstOrDefault(c => 
            c.Nome.Equals("Outros", StringComparison.OrdinalIgnoreCase));

        if (categoriaOutros == null)
        {
            // Criar categoria "Outros" se não existir
            categoriaOutros = new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Outros",
                Descricao = "Produtos sem categoria específica",
                CreatedAt = DateTime.UtcNow
            };
            await _categoriaRepository.AddAsync(categoriaOutros, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Categoria 'Outros' criada automaticamente");
        }

        _categoriaOutrosId = categoriaOutros.Id;
        return _categoriaOutrosId.Value;
    }
}
