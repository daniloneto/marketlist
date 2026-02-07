using System.Text;
using System.Text.Json;
using MarketList.Application.Interfaces;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

/// <summary>
/// Serviço do assistente de chat integrado com MCP
/// Busca dados reais do banco e envia como contexto ao LLM
/// </summary>
public class ChatAssistantService : IChatAssistantService
{
    private readonly IMcpClientService _mcpClient;
    private readonly ILogger<ChatAssistantService> _logger;
    private readonly IListaDeComprasRepository _listaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IHistoricoPrecoRepository _historicoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IEmpresaRepository _empresaRepository;

    public ChatAssistantService(
        IMcpClientService mcpClient,
        ILogger<ChatAssistantService> logger,
        IListaDeComprasRepository listaRepository,
        IProdutoRepository produtoRepository,
        IHistoricoPrecoRepository historicoRepository,
        ICategoriaRepository categoriaRepository,
        IEmpresaRepository empresaRepository)
    {
        _mcpClient = mcpClient;
        _logger = logger;
        _listaRepository = listaRepository;
        _produtoRepository = produtoRepository;
        _historicoRepository = historicoRepository;
        _categoriaRepository = categoriaRepository;
        _empresaRepository = empresaRepository;

        InitializeTools();
    }

    public async Task<string> ProcessMessageAsync(
        string userId,
        string userMessage,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processando mensagem do usuário: {UserId}", userId);
            
            // Busca dados relevantes do banco baseado na mensagem
            var contextData = await GatherContextDataAsync(userMessage, cancellationToken);
            
            // Monta mensagem enriquecida com contexto real
            var enrichedMessage = BuildEnrichedMessage(userMessage, contextData);
            
            // Adiciona mensagem ao histórico
            conversationHistory.Add(new ChatMessage { Role = "user", Content = enrichedMessage });

            // Obtém resposta do LLM
            var response = await _mcpClient.SendMessageAsync(enrichedMessage, conversationHistory, cancellationToken);

            _logger.LogInformation("Resposta recebida do LLM");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamProcessMessageAsync(
        string userId,
        string userMessage,
        List<ChatMessage> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stream processando mensagem do usuário: {UserId}", userId);
        
        // Busca dados relevantes do banco
        var contextData = await GatherContextDataAsync(userMessage, cancellationToken);
        var enrichedMessage = BuildEnrichedMessage(userMessage, contextData);
        
        conversationHistory.Add(new ChatMessage { Role = "user", Content = enrichedMessage });

        await foreach (var chunk in _mcpClient.StreamResponseAsync(enrichedMessage, conversationHistory, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }

        _logger.LogInformation("Stream finalizado");
    }

    /// <summary>
    /// Busca dados relevantes do banco baseado na intenção da mensagem
    /// </summary>
    private async Task<ContextData> GatherContextDataAsync(string userMessage, CancellationToken cancellationToken)
    {
        var context = new ContextData();
        var messageLower = userMessage.ToLowerInvariant();
        
        // Detecta intenção e busca dados relevantes
        if (ContainsAny(messageLower, "lista", "compra", "compras", "últimas", "minhas", "mostrar", "ver"))
        {
            var listas = await _listaRepository.GetAllAsync(cancellationToken);
            context.Listas = listas.OrderByDescending(l => l.CreatedAt).Take(5).ToList();
            _logger.LogDebug("Buscou {Count} listas de compras", context.Listas.Count);
        }
        
        if (ContainsAny(messageLower, "produto", "produtos", "buscar", "procurar", "encontrar"))
        {
            var produtos = await _produtoRepository.GetAllAsync(cancellationToken);
            context.Produtos = produtos.Take(20).ToList();
            _logger.LogDebug("Buscou {Count} produtos", context.Produtos.Count);
        }
        
        if (ContainsAny(messageLower, "categoria", "categorias", "tipo", "tipos"))
        {
            var categorias = await _categoriaRepository.GetAllAsync(cancellationToken);
            context.Categorias = categorias.ToList();
            _logger.LogDebug("Buscou {Count} categorias", context.Categorias.Count);
        }
        
        if (ContainsAny(messageLower, "loja", "lojas", "mercado", "supermercado", "empresa"))
        {
            var empresas = await _empresaRepository.GetAllAsync(cancellationToken);
            context.Empresas = empresas.ToList();
            _logger.LogDebug("Buscou {Count} empresas", context.Empresas.Count);
        }
        
        if (ContainsAny(messageLower, "preço", "preco", "valor", "custo", "histórico", "historico"))
        {
            // Busca histórico dos últimos produtos
            var produtos = await _produtoRepository.GetAllAsync(cancellationToken);
            var produtosComHistorico = new List<(Domain.Entities.Produto produto, List<Domain.Entities.HistoricoPreco> historico)>();
            
            foreach (var produto in produtos.Take(5))
            {
                var historico = await _historicoRepository.GetByProdutoIdAsync(produto.Id, 90, cancellationToken);
                if (historico.Any())
                {
                    produtosComHistorico.Add((produto, historico.OrderByDescending(h => h.DataConsulta).Take(3).ToList()));
                }
            }
            context.HistoricoPrecos = produtosComHistorico;
        }
        
        return context;
    }
    
    /// <summary>
    /// Monta a mensagem enriquecida com dados reais do sistema
    /// </summary>
    private string BuildEnrichedMessage(string userMessage, ContextData context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CONTEXTO DO SISTEMA (dados reais do banco de dados):");
        sb.AppendLine("=".PadRight(50, '='));
        
        if (context.Listas?.Any() == true)
        {
            sb.AppendLine("\n📋 LISTAS DE COMPRAS RECENTES:");
            foreach (var lista in context.Listas)
            {
                sb.AppendLine($"  - ID: {lista.Id}");
                sb.AppendLine($"    Nome: {lista.Nome ?? "Sem nome"}");
                sb.AppendLine($"    Data: {lista.CreatedAt:dd/MM/yyyy}");
                sb.AppendLine($"    Status: {lista.Status}");
                if (lista.Itens?.Any() == true)
                {
                    sb.AppendLine($"    Itens ({lista.Itens.Count}):");
                    foreach (var item in lista.Itens.Take(10))
                    {
                        var produtoNome = item.Produto?.Nome ?? "Produto sem nome";
                        sb.AppendLine($"      • {produtoNome} - Qtd: {item.Quantidade} {item.UnidadeDeMedida}");
                    }
                }
                sb.AppendLine();
            }
        }
        
        if (context.Produtos?.Any() == true)
        {
            sb.AppendLine("\n🛒 PRODUTOS DISPONÍVEIS:");
            foreach (var produto in context.Produtos.Take(15))
            {
                sb.AppendLine($"  - {produto.Nome} (ID: {produto.Id})");
            }
        }
        
        if (context.Categorias?.Any() == true)
        {
            sb.AppendLine("\n🏷️ CATEGORIAS:");
            sb.AppendLine($"  {string.Join(", ", context.Categorias.Select(c => c.Nome))}");
        }
        
        if (context.Empresas?.Any() == true)
        {
            sb.AppendLine("\n🏪 LOJAS/SUPERMERCADOS:");
            foreach (var empresa in context.Empresas)
            {
                sb.AppendLine($"  - {empresa.Nome}");
            }
        }
        
        if (context.HistoricoPrecos?.Any() == true)
        {
            sb.AppendLine("\n💰 HISTÓRICO DE PREÇOS:");
            foreach (var (produto, historico) in context.HistoricoPrecos)
            {
                sb.AppendLine($"  {produto.Nome}:");
                foreach (var h in historico)
                {
                    sb.AppendLine($"    • R$ {h.PrecoUnitario:F2} em {h.DataConsulta:dd/MM/yyyy}");
                }
            }
        }
        
        sb.AppendLine("\n" + "=".PadRight(50, '='));
        sb.AppendLine("\nPERGUNTA DO USUÁRIO:");
        sb.AppendLine(userMessage);
        sb.AppendLine("\nResponda de forma clara e objetiva, usando APENAS os dados fornecidos acima. Se não houver dados relevantes, informe que não há registros disponíveis.");
        
        return sb.ToString();
    }
    
    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    public List<ToolDefinition> GetAvailableTools()
    {
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "get_shopping_lists",
                Description = "Retorna as últimas listas de compras do usuário",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    {
                        "limit", new ParameterDefinition
                        {
                            Type = "number",
                            Description = "Número máximo de listas a retornar",
                            Required = false
                        }
                    }
                }
            },
            new()
            {
                Name = "get_list_details",
                Description = "Retorna detalhes completos de uma lista de compras específica",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    {
                        "list_id", new ParameterDefinition
                        {
                            Type = "string",
                            Description = "ID da lista de compras",
                            Required = true
                        }
                    }
                }
            },
            new()
            {
                Name = "search_products",
                Description = "Busca produtos por nome ou categoria",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    {
                        "query", new ParameterDefinition
                        {
                            Type = "string",
                            Description = "Termo de busca (nome ou categoria)",
                            Required = true
                        }
                    },
                    {
                        "limit", new ParameterDefinition
                        {
                            Type = "number",
                            Description = "Número máximo de produtos a retornar",
                            Required = false
                        }
                    }
                }
            },
            new()
            {
                Name = "get_price_history",
                Description = "Retorna histórico de preços de um produto",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    {
                        "product_id", new ParameterDefinition
                        {
                            Type = "string",
                            Description = "ID do produto",
                            Required = true
                        }
                    },
                    {
                        "days", new ParameterDefinition
                        {
                            Type = "number",
                            Description = "Número de dias para retornar histórico",
                            Required = false
                        }
                    }
                }
            },
            new()
            {
                Name = "get_categories",
                Description = "Lista todas as categorias disponíveis",
                Parameters = new Dictionary<string, ParameterDefinition>()
            },
            new()
            {
                Name = "get_stores",
                Description = "Lista todos os supermercados/empresas cadastrados",
                Parameters = new Dictionary<string, ParameterDefinition>()
            }
        };

        return tools;
    }

    private void InitializeTools()
    {
        var tools = GetAvailableTools();
        _mcpClient.SetTools(tools);
        _logger.LogInformation("Assistente de chat inicializado com {ToolCount} tools", tools.Count);
    }
}

/// <summary>
/// Dados de contexto buscados do banco para enriquecer o prompt
/// </summary>
internal class ContextData
{
    public List<Domain.Entities.ListaDeCompras>? Listas { get; set; }
    public List<Domain.Entities.Produto>? Produtos { get; set; }
    public List<Domain.Entities.Categoria>? Categorias { get; set; }
    public List<Domain.Entities.Empresa>? Empresas { get; set; }
    public List<(Domain.Entities.Produto produto, List<Domain.Entities.HistoricoPreco> historico)>? HistoricoPrecos { get; set; }
}
