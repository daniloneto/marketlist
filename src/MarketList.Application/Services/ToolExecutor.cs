using System.Text.Json;
using System.Text.Json.Serialization;
using MarketList.Application.Interfaces;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

/// <summary>
/// Responsável por executar ferramentas (tools) solicitadas pelo LLM
/// </summary>
public class ToolExecutor
{
    private readonly IListaDeComprasRepository _listaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IHistoricoPrecoRepository _historicoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(
        IListaDeComprasRepository listaRepository,
        IProdutoRepository produtoRepository,
        IHistoricoPrecoRepository historicoRepository,
        ICategoriaRepository categoriaRepository,
        IEmpresaRepository empresaRepository,
        ILogger<ToolExecutor> logger)
    {
        _listaRepository = listaRepository;
        _produtoRepository = produtoRepository;
        _historicoRepository = historicoRepository;
        _categoriaRepository = categoriaRepository;
        _empresaRepository = empresaRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executa uma tool baseado no nome e parâmetros
    /// </summary>
    public async Task<string> ExecuteAsync(
        string toolName,
        Dictionary<string, object> parameters,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return toolName switch
            {
                "get_shopping_lists" => await GetShoppingListsAsync(userId, parameters, cancellationToken),
                "get_list_details" => await GetListDetailsAsync(parameters, cancellationToken),
                "search_products" => await SearchProductsAsync(parameters, cancellationToken),
                "get_price_history" => await GetPriceHistoryAsync(parameters, cancellationToken),
                "get_categories" => await GetCategoriesAsync(cancellationToken),
                "get_stores" => await GetStoresAsync(cancellationToken),
                _ => throw new ArgumentException($"Tool desconhecida: {toolName}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar tool: {ToolName}", toolName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> GetShoppingListsAsync(
        string userId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var limit = parameters.TryGetValue("limit", out var lim) && lim is int l ? l : 10;

        var lists = await _listaRepository.GetByUsuarioIdAsync(userId, limit, cancellationToken);
        
        var result = lists.Select(l => new
        {
            id = l.Id,
            nome = l.Nome,
            textoOriginal = l.TextoOriginal,
            dataCriacao = l.CreatedAt,
            dataAtualizacao = l.UpdatedAt,
            status = l.Status
        }).ToList();

        _logger.LogInformation("Retornadas {Count} listas para usuário {UserId}", result.Count, userId);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private async Task<string> GetListDetailsAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("list_id", out var listIdObj) || !Guid.TryParse(listIdObj?.ToString(), out var listId))
        {
            return JsonSerializer.Serialize(new { error = "list_id inválido" });
        }

        var list = await _listaRepository.GetWithItensAsync(listId, cancellationToken);
        if (list == null)
        {
            return JsonSerializer.Serialize(new { error = "Lista não encontrada" });
        }

        var result = new
        {
            id = list.Id,
            nome = list.Nome,
            itens = list.Itens?.Select(i => new
            {
                i.Id,
                nomeProduto = i.Produto.Nome,
                i.Quantidade,
                unidadeMedida = i.UnidadeDeMedida,
                precoUnitario = i.PrecoUnitario,
                precoTotal = i.PrecoTotal,
                categoria = i.Produto.Categoria.Nome,
                comprado = i.Comprado
            }).ToList()
        };

        _logger.LogInformation("Retornados detalhes da lista {ListId}", listId);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private async Task<string> SearchProductsAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("query", out var queryObj))
        {
            return JsonSerializer.Serialize(new { error = "query é obrigatório" });
        }

        var query = queryObj?.ToString() ?? "";
        var limit = parameters.TryGetValue("limit", out var lim) && lim is int l ? l : 10;

        var products = await _produtoRepository.FindSimilarByNameAsync(query, limit, cancellationToken);

        var result = products.Select(p => new
        {
            id = p.Id,
            nome = p.Nome,
            categoria = p.Categoria.Nome,
            codigoLoja = p.CodigoLoja,
            descricao = p.Descricao
        }).ToList();

        _logger.LogInformation("Encontrados {Count} produtos para query: {Query}", result.Count, query);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private async Task<string> GetPriceHistoryAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("product_id", out var productIdObj) || !Guid.TryParse(productIdObj?.ToString(), out var productId))
        {
            return JsonSerializer.Serialize(new { error = "product_id inválido" });
        }

        var days = parameters.TryGetValue("days", out var d) && d is int n ? n : 90;

        var history = await _historicoRepository.GetByProdutoIdAsync(productId, days, cancellationToken);

        var result = history.Select(h => new
        {
            id = h.Id,
            preco = h.PrecoUnitario,
            dataConsulta = h.DataConsulta,
            fontePreco = h.FontePreco,
            empresa = h.Empresa?.Nome ?? "Desconhecida"
        }).OrderByDescending(h => h.dataConsulta).ToList();

        _logger.LogInformation("Retornado histórico de {Count} preços para produto {ProductId}", result.Count, productId);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private async Task<string> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoriaRepository.GetAllAsync(cancellationToken);

        var result = categories.Select(c => new
        {
            id = c.Id,
            nome = c.Nome
        }).ToList();

        _logger.LogInformation("Retornadas {Count} categorias", result.Count);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private async Task<string> GetStoresAsync(CancellationToken cancellationToken)
    {
        var stores = await _empresaRepository.GetAllAsync(cancellationToken);

        var result = stores.Select(e => new
        {
            id = e.Id,
            nome = e.Nome
        }).ToList();

        _logger.LogInformation("Retornadas {Count} empresas", result.Count);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
