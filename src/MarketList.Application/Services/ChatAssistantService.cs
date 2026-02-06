using System.Text.Json;
using MarketList.Application.Interfaces;
using MarketList.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

/// <summary>
/// Serviço do assistente de chat integrado com MCP
/// Orquestra tools e comunicação com LLM
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
            
            // Adiciona mensagem do usuário ao histórico
            conversationHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

            // Obtém resposta do MCP
            var response = await _mcpClient.SendMessageAsync(userMessage, conversationHistory, cancellationToken);

            _logger.LogInformation("Resposta recebida do MCP");
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
        conversationHistory.Add(new ChatMessage { Role = "user", Content = userMessage });

        try
        {
            _logger.LogInformation("Stream processando mensagem do usuário: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar stream de mensagem");
        }

        await foreach (var chunk in _mcpClient.StreamResponseAsync(userMessage, conversationHistory, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }

        try
        {
            _logger.LogInformation("Stream finalizado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer stream de mensagem");
        }
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
