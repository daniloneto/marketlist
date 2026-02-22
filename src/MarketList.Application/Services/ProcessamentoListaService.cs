using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Enums;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

public class ProcessamentoListaService : IProcessamentoListaService
{
    private readonly IRepository<ListaDeCompras> _listaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IRepository<Categoria> _categoriaRepository;
    private readonly IHistoricoPrecoRepository _historicoPrecoRepository;
    private readonly IRepository<ItemListaDeCompras> _itemRepository;
    private readonly IAnalisadorTextoService _analisadorTexto;
    private readonly ILeitorNotaFiscal _leitorNotaFiscal;
    private readonly IProdutoResolverService _produtoResolver;
    private readonly IProductResolutionService _productResolutionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessamentoListaService> _logger;

    public ProcessamentoListaService(
        IRepository<ListaDeCompras> listaRepository,
        IProdutoRepository produtoRepository,
        IRepository<Categoria> categoriaRepository,
        IHistoricoPrecoRepository historicoPrecoRepository,
        IRepository<ItemListaDeCompras> itemRepository,
        IAnalisadorTextoService analisadorTexto,
        ILeitorNotaFiscal leitorNotaFiscal,
        IProdutoResolverService produtoResolver,
        IProductResolutionService productResolutionService,
        IUnitOfWork unitOfWork,
        ILogger<ProcessamentoListaService> logger)
    {
        _listaRepository = listaRepository;
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _historicoPrecoRepository = historicoPrecoRepository;
        _itemRepository = itemRepository;
        _analisadorTexto = analisadorTexto;
        _leitorNotaFiscal = leitorNotaFiscal;
        _produtoResolver = produtoResolver;
        _productResolutionService = productResolutionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessarListaAsync(Guid listaId, CancellationToken cancellationToken = default)
    {
        var lista = await _listaRepository.GetByIdAsync(listaId, cancellationToken);
        if (lista == null)
        {
            _logger.LogError("Lista {ListaId} não encontrada", listaId);
            return;
        }

        try
        {
            lista.Status = StatusLista.Processando;
            await _listaRepository.UpdateAsync(lista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (lista.TipoEntrada == TipoEntrada.NotaFiscal)
            {
                await ProcessarNotaFiscalAsync(lista, cancellationToken);
            }
            else
            {
                await ProcessarListaSimplesAsync(lista, cancellationToken);
            }

            lista.Status = StatusLista.Concluida;
            lista.ProcessadoEm = DateTime.UtcNow;
            lista.ErroProcessamento = null;
            await _listaRepository.UpdateAsync(lista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar lista {ListaId}", listaId);
            lista.Status = StatusLista.Erro;
            lista.ErroProcessamento = ex.Message;
            await _listaRepository.UpdateAsync(lista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task ProcessarListaSimplesAsync(ListaDeCompras lista, CancellationToken cancellationToken)
    {
        var itensAnalisados = _analisadorTexto.AnalisarTexto(lista.TextoOriginal ?? string.Empty);

        foreach (var itemAnalisado in itensAnalisados)
        {
            var nomeCategoria = _analisadorTexto.DetectarCategoria(itemAnalisado.NomeProduto);
            var categoria = await ObterOuCriarCategoriaAsync(nomeCategoria, cancellationToken);
            var produto = await ObterOuCriarProdutoAsync(itemAnalisado.NomeProduto, itemAnalisado.Unidade, null, categoria, cancellationToken);
            var precoUnitario = await ObterUltimoPrecoAsync(produto.Id, cancellationToken);

            var itemLista = new ItemListaDeCompras
            {
                Id = Guid.NewGuid(),
                ListaDeComprasId = lista.Id,
                ProdutoId = produto.Id,
                Quantidade = itemAnalisado.Quantidade,
                PrecoUnitario = precoUnitario,
                TextoOriginal = itemAnalisado.TextoOriginal,
                Comprado = false,
                CreatedAt = DateTime.UtcNow
            };

            await _itemRepository.AddAsync(itemLista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessarNotaFiscalAsync(ListaDeCompras lista, CancellationToken cancellationToken)
    {
        var itensNota = _leitorNotaFiscal.Ler(lista.TextoOriginal ?? string.Empty);
        var catalogSnapshot = await _productResolutionService.GetActiveCatalogSnapshotAsync(cancellationToken);

        foreach (var itemNota in itensNota)
        {
            try
            {
                var resolution = await _productResolutionService.ResolveAsync(itemNota.NomeProduto, catalogSnapshot, cancellationToken);
                var categoria = await ObterOuCriarCategoriaAsync(resolution.CategoryName, cancellationToken);

                var produto = await ObterOuCriarProdutoAsync(
                    resolution.ResolvedName ?? itemNota.NomeProduto,
                    itemNota.UnidadeDeMedida.ToString(),
                    itemNota.CodigoLoja,
                    categoria,
                    cancellationToken);

                var dataHistorico = lista.DataCompra ?? DateTime.UtcNow;
                await _historicoPrecoRepository.AddAsync(new HistoricoPreco
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produto.Id,
                    PrecoUnitario = itemNota.PrecoUnitario,
                    DataConsulta = dataHistorico,
                    FontePreco = "NotaFiscal",
                    EmpresaId = lista.EmpresaId,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                await _itemRepository.AddAsync(new ItemListaDeCompras
                {
                    Id = Guid.NewGuid(),
                    ListaDeComprasId = lista.Id,
                    ProdutoId = produto.Id,
                    Quantidade = itemNota.Quantidade,
                    UnidadeDeMedida = itemNota.UnidadeDeMedida,
                    PrecoUnitario = itemNota.PrecoUnitario,
                    PrecoTotal = itemNota.PrecoTotal,
                    TextoOriginal = itemNota.TextoOriginal,
                    RawName = itemNota.NomeProduto,
                    ResolvedName = resolution.ResolvedName,
                    ResolvedCategoryId = produto.CategoriaId,
                    MatchScore = resolution.Score,
                    ResolutionStatus = resolution.Status,
                    Comprado = false,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar item da nota: {Produto}", itemNota.NomeProduto);
            }
        }
    }

    private async Task<Categoria> ObterOuCriarCategoriaAsync(string? nomeCategoria, CancellationToken cancellationToken)
    {
        nomeCategoria = string.IsNullOrWhiteSpace(nomeCategoria) ? "Sem Categoria" : nomeCategoria.Trim();

        var categorias = await _categoriaRepository.GetAllAsync(cancellationToken);
        var categoria = categorias.FirstOrDefault(c => c.Nome.Equals(nomeCategoria, StringComparison.OrdinalIgnoreCase));

        if (categoria != null)
        {
            return categoria;
        }

        categoria = new Categoria { Id = Guid.NewGuid(), Nome = nomeCategoria, CreatedAt = DateTime.UtcNow };
        await _categoriaRepository.AddAsync(categoria, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return categoria;
    }

    private async Task<Produto> ObterOuCriarProdutoAsync(string nomeProduto, string? unidade, string? codigoLoja, Categoria categoria, CancellationToken cancellationToken)
    {
        var resolucao = await _produtoResolver.ResolverProdutoAsync(nomeProduto, codigoLoja, categoria.Id, cancellationToken);
        var produto = resolucao.Produto;

        if (produto.CategoriaId != categoria.Id)
        {
            produto.CategoriaId = categoria.Id;
            produto.CategoriaPrecisaRevisao = true;
            await _produtoRepository.UpdateAsync(produto, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(unidade) && string.IsNullOrWhiteSpace(produto.Unidade))
        {
            produto.Unidade = unidade;
            await _produtoRepository.UpdateAsync(produto, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return produto;
    }

    private async Task<decimal?> ObterUltimoPrecoAsync(Guid produtoId, CancellationToken cancellationToken)
    {
        var ultimoHistorico = await _historicoPrecoRepository.GetLatestByProdutoIdAsync(produtoId, cancellationToken);
        return ultimoHistorico?.PrecoUnitario;
    }
}
