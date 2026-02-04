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
    private readonly IRepository<Produto> _produtoRepository;
    private readonly IRepository<Categoria> _categoriaRepository;
    private readonly IRepository<HistoricoPreco> _historicoPrecoRepository;
    private readonly IRepository<ItemListaDeCompras> _itemRepository;
    private readonly IAnalisadorTextoService _analisadorTexto;
    private readonly IPriceLookupService _priceLookupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessamentoListaService> _logger;

    // Coordenadas padrão - Salvador/BA (podem vir de configuração)
    private const double DefaultLatitude = -12.9714;
    private const double DefaultLongitude = -38.5014;

    public ProcessamentoListaService(
        IRepository<ListaDeCompras> listaRepository,
        IRepository<Produto> produtoRepository,
        IRepository<Categoria> categoriaRepository,
        IRepository<HistoricoPreco> historicoPrecoRepository,
        IRepository<ItemListaDeCompras> itemRepository,
        IAnalisadorTextoService analisadorTexto,
        IPriceLookupService priceLookupService,
        IUnitOfWork unitOfWork,
        ILogger<ProcessamentoListaService> logger)
    {
        _listaRepository = listaRepository;
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _historicoPrecoRepository = historicoPrecoRepository;
        _itemRepository = itemRepository;
        _analisadorTexto = analisadorTexto;
        _priceLookupService = priceLookupService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessarListaAsync(Guid listaId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processamento da lista {ListaId}", listaId);

        var lista = await _listaRepository.GetByIdAsync(listaId, cancellationToken);
        if (lista == null)
        {
            _logger.LogError("Lista {ListaId} não encontrada", listaId);
            return;
        }

        try
        {
            // Atualiza status para processando
            lista.Status = StatusLista.Processando;
            await _listaRepository.UpdateAsync(lista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 1. Analisar o texto da lista
            var itensAnalisados = _analisadorTexto.AnalisarTexto(lista.TextoOriginal ?? string.Empty);
            _logger.LogInformation("Analisados {Count} itens da lista {ListaId}", itensAnalisados.Count, listaId);

            foreach (var itemAnalisado in itensAnalisados)
            {
                await ProcessarItemAsync(lista, itemAnalisado, cancellationToken);
            }

            // Finaliza o processamento
            lista.Status = StatusLista.Concluida;
            lista.ProcessadoEm = DateTime.UtcNow;
            lista.ErroProcessamento = null;
            await _listaRepository.UpdateAsync(lista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lista {ListaId} processada com sucesso", listaId);
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

    private async Task ProcessarItemAsync(ListaDeCompras lista, ItemAnalisadoDto itemAnalisado, CancellationToken cancellationToken)
    {
        // 2. Detectar e criar categoria se necessário
        var nomeCategoria = _analisadorTexto.DetectarCategoria(itemAnalisado.NomeProduto);
        var categoria = await ObterOuCriarCategoriaAsync(nomeCategoria, cancellationToken);

        // 3. Verificar/criar produto
        var produto = await ObterOuCriarProdutoAsync(itemAnalisado, categoria, cancellationToken);

        // 4. Consultar preço externo usando o novo PriceLookupService
        decimal? precoUnitario = null;
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
                // Registrar no histórico de preços
                var historicoPreco = new HistoricoPreco
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produto.Id,
                    PrecoUnitario = resultado.Price.Value,
                    DataConsulta = resultado.Date ?? DateTime.UtcNow,
                    FontePreco = $"Preço da Hora - {resultado.StoreName ?? "N/A"}",
                    CreatedAt = DateTime.UtcNow
                };
                await _historicoPrecoRepository.AddAsync(historicoPreco, cancellationToken);
                precoUnitario = resultado.Price;

                _logger.LogInformation(
                    "Preço registrado para {Produto}: R$ {Preco:N2} em {Loja}",
                    produto.Nome, resultado.Price, resultado.StoreName ?? "N/A");
            }
            else
            {
                _logger.LogInformation("Preço não encontrado para {Produto}", produto.Nome);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao consultar preço para {Produto}", produto.Nome);
            // Não falha o processamento se a consulta de preço falhar
        }

        // Se não conseguiu preço externo, busca o último preço conhecido
        if (!precoUnitario.HasValue)
        {
            var ultimoPreco = await ObterUltimoPrecoAsync(produto.Id, cancellationToken);
            precoUnitario = ultimoPreco;
        }

        // 5. Criar item da lista
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

        _logger.LogInformation("Item adicionado: {Produto} x {Quantidade}", produto.Nome, itemAnalisado.Quantidade);
    }

    private async Task<Categoria> ObterOuCriarCategoriaAsync(string nomeCategoria, CancellationToken cancellationToken)
    {
        var categorias = await _categoriaRepository.GetAllAsync(cancellationToken);
        var categoria = categorias.FirstOrDefault(c => 
            c.Nome.Equals(nomeCategoria, StringComparison.OrdinalIgnoreCase));

        if (categoria == null)
        {
            categoria = new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = nomeCategoria,
                CreatedAt = DateTime.UtcNow
            };
            await _categoriaRepository.AddAsync(categoria, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Categoria criada: {Categoria}", nomeCategoria);
        }

        return categoria;
    }

    private async Task<Produto> ObterOuCriarProdutoAsync(ItemAnalisadoDto itemAnalisado, Categoria categoria, CancellationToken cancellationToken)
    {
        var produtos = await _produtoRepository.GetAllAsync(cancellationToken);
        var produto = produtos.FirstOrDefault(p => 
            p.Nome.Equals(itemAnalisado.NomeProduto, StringComparison.OrdinalIgnoreCase));

        if (produto == null)
        {
            produto = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = itemAnalisado.NomeProduto,
                Unidade = itemAnalisado.Unidade,
                CategoriaId = categoria.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _produtoRepository.AddAsync(produto, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Produto criado: {Produto} na categoria {Categoria}", 
                itemAnalisado.NomeProduto, categoria.Nome);
        }

        return produto;
    }

    private async Task<decimal?> ObterUltimoPrecoAsync(Guid produtoId, CancellationToken cancellationToken)
    {
        var historico = await _historicoPrecoRepository.GetAllAsync(cancellationToken);
        var ultimoPreco = historico
            .Where(h => h.ProdutoId == produtoId)
            .OrderByDescending(h => h.DataConsulta)
            .FirstOrDefault();

        return ultimoPreco?.PrecoUnitario;
    }
}
