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
    private readonly ILeitorNotaFiscal _leitorNotaFiscal;
    private readonly IPrecoExternoApi _precoExternoApi;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessamentoListaService> _logger;

    public ProcessamentoListaService(
        IRepository<ListaDeCompras> listaRepository,
        IRepository<Produto> produtoRepository,
        IRepository<Categoria> categoriaRepository,
        IRepository<HistoricoPreco> historicoPrecoRepository,
        IRepository<ItemListaDeCompras> itemRepository,
        IAnalisadorTextoService analisadorTexto,
        ILeitorNotaFiscal leitorNotaFiscal,
        IPrecoExternoApi precoExternoApi,
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
        _precoExternoApi = precoExternoApi;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }    public async Task ProcessarListaAsync(Guid listaId, CancellationToken cancellationToken = default)
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

            // Escolhe o pipeline baseado no tipo de entrada
            if (lista.TipoEntrada == TipoEntrada.NotaFiscal)
            {
                await ProcessarNotaFiscalAsync(lista, cancellationToken);
            }
            else
            {
                await ProcessarListaSimplesAsync(lista, cancellationToken);
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

    /// <summary>
    /// Pipeline para processar lista simples (texto livre com nomes de produtos)
    /// </summary>
    private async Task ProcessarListaSimplesAsync(ListaDeCompras lista, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processando lista simples {ListaId}", lista.Id);

        // 1. Analisar o texto da lista
        var itensAnalisados = _analisadorTexto.AnalisarTexto(lista.TextoOriginal ?? string.Empty);
        _logger.LogInformation("Analisados {Count} itens da lista {ListaId}", itensAnalisados.Count, lista.Id);

        foreach (var itemAnalisado in itensAnalisados)
        {
            await ProcessarItemListaSimplesAsync(lista, itemAnalisado, cancellationToken);
        }
    }

    /// <summary>
    /// Pipeline para processar nota fiscal (texto estruturado com preços e quantidades)
    /// </summary>
    private async Task ProcessarNotaFiscalAsync(ListaDeCompras lista, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processando nota fiscal {ListaId}", lista.Id);

        // 1. Ler e interpretar a nota fiscal
        var itensNota = _leitorNotaFiscal.Ler(lista.TextoOriginal ?? string.Empty);
        _logger.LogInformation("Lidos {Count} itens da nota fiscal {ListaId}", itensNota.Count, lista.Id);

        foreach (var itemNota in itensNota)
        {
            await ProcessarItemNotaFiscalAsync(lista, itemNota, cancellationToken);
        }
    }    private async Task ProcessarItemListaSimplesAsync(ListaDeCompras lista, ItemAnalisadoDto itemAnalisado, CancellationToken cancellationToken)
    {
        // 2. Detectar e criar categoria se necessário
        var nomeCategoria = _analisadorTexto.DetectarCategoria(itemAnalisado.NomeProduto);
        var categoria = await ObterOuCriarCategoriaAsync(nomeCategoria, cancellationToken);

        // 3. Verificar/criar produto
        var produto = await ObterOuCriarProdutoAsync(itemAnalisado.NomeProduto, itemAnalisado.Unidade, null, categoria, cancellationToken);

        // 4. Consultar preço externo
        decimal? precoUnitario = null;
        try
        {
            var precoExterno = await _precoExternoApi.ConsultarPrecoAsync(produto.Nome, cancellationToken);
            if (precoExterno.Sucesso && precoExterno.Preco.HasValue)
            {
                // Registrar no histórico de preços
                var historicoPreco = new HistoricoPreco
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produto.Id,
                    PrecoUnitario = precoExterno.Preco.Value,
                    DataConsulta = DateTime.UtcNow,
                    FontePreco = precoExterno.Fonte,
                    EmpresaId = lista.EmpresaId,
                    CreatedAt = DateTime.UtcNow
                };
                await _historicoPrecoRepository.AddAsync(historicoPreco, cancellationToken);
                precoUnitario = precoExterno.Preco;

                _logger.LogInformation("Preço registrado para {Produto}: {Preco}", produto.Nome, precoExterno.Preco);
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

    /// <summary>
    /// Processa um item vindo de uma nota fiscal
    /// </summary>
    private async Task ProcessarItemNotaFiscalAsync(ListaDeCompras lista, ItemNotaFiscalLidoDto itemNota, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Detectar ou usar categoria genérica
            var nomeCategoria = _analisadorTexto.DetectarCategoria(itemNota.NomeProduto);
            var categoria = await ObterOuCriarCategoriaAsync(nomeCategoria, cancellationToken);

            // 2. Encontrar ou criar produto usando código da loja + nome
            var produto = await ObterOuCriarProdutoAsync(
                itemNota.NomeProduto, 
                itemNota.UnidadeDeMedida.ToString(), 
                itemNota.CodigoLoja, 
                categoria, 
                cancellationToken);

            // 3. Registrar no histórico de preços
            var historicoPreco = new HistoricoPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                PrecoUnitario = itemNota.PrecoUnitario,
                DataConsulta = DateTime.UtcNow,
                FontePreco = "NotaFiscal",
                EmpresaId = lista.EmpresaId,
                CreatedAt = DateTime.UtcNow
            };
            await _historicoPrecoRepository.AddAsync(historicoPreco, cancellationToken);

            // 4. Criar item da lista com todas as informações da nota
            var itemLista = new ItemListaDeCompras
            {
                Id = Guid.NewGuid(),
                ListaDeComprasId = lista.Id,
                ProdutoId = produto.Id,
                Quantidade = itemNota.Quantidade,
                UnidadeDeMedida = itemNota.UnidadeDeMedida,
                PrecoUnitario = itemNota.PrecoUnitario,
                PrecoTotal = itemNota.PrecoTotal,
                TextoOriginal = itemNota.TextoOriginal,
                Comprado = false, // Lista da nota fiscal = compras já realizadas, mas mantendo false para compatibilidade
                CreatedAt = DateTime.UtcNow
            };

            await _itemRepository.AddAsync(itemLista, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Item da nota adicionado: {Produto} - {Qtd} {Unidade} - R$ {Preco}", 
                itemNota.NomeProduto, itemNota.Quantidade, itemNota.UnidadeDeMedida, itemNota.PrecoUnitario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar item da nota: {Produto}", itemNota.NomeProduto);
            // Continua processando os próximos itens (tolerante a falhas)
        }
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
    }    private async Task<Produto> ObterOuCriarProdutoAsync(
        string nomeProduto, 
        string? unidade, 
        string? codigoLoja, 
        Categoria categoria, 
        CancellationToken cancellationToken)
    {
        var produtos = await _produtoRepository.GetAllAsync(cancellationToken);
        
        // Se temos código da loja, usa como critério principal
        Produto? produto = null;
        if (!string.IsNullOrWhiteSpace(codigoLoja))
        {
            produto = produtos.FirstOrDefault(p => 
                !string.IsNullOrWhiteSpace(p.CodigoLoja) && 
                p.CodigoLoja.Equals(codigoLoja, StringComparison.OrdinalIgnoreCase));
        }
        
        // Se não encontrou por código, busca por nome
        if (produto == null)
        {
            produto = produtos.FirstOrDefault(p => 
                p.Nome.Equals(nomeProduto, StringComparison.OrdinalIgnoreCase));
        }

        if (produto == null)
        {
            produto = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = nomeProduto,
                Unidade = unidade,
                CodigoLoja = codigoLoja,
                CategoriaId = categoria.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _produtoRepository.AddAsync(produto, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Produto criado: {Produto} (Código: {Codigo}) na categoria {Categoria}", 
                nomeProduto, codigoLoja ?? "N/A", categoria.Nome);
        }
        else if (!string.IsNullOrWhiteSpace(codigoLoja) && string.IsNullOrWhiteSpace(produto.CodigoLoja))
        {
            // Atualiza o código da loja se o produto já existia mas não tinha código
            produto.CodigoLoja = codigoLoja;
            await _produtoRepository.UpdateAsync(produto, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Código da loja atualizado para produto: {Produto} -> {Codigo}", 
                nomeProduto, codigoLoja);
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
