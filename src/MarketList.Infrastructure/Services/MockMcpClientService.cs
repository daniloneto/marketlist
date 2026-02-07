using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Infrastructure.Services;

/// <summary>
/// Serviço MCP Mock para desenvolvimento/testes sem Ollama
/// Retorna respostas pré-definidas baseadas nas ferramentas solicitadas
/// </summary>
public class MockMcpClientService : IMcpClientService
{
    private readonly ILogger<MockMcpClientService> _logger;
    private List<ToolDefinition> _tools = [];

    public MockMcpClientService(ILogger<MockMcpClientService> logger)
    {
        _logger = logger;
    }

    public void SetTools(List<ToolDefinition> tools)
    {
        _tools = tools;
        _logger.LogInformation("Mock MCP: Ferramentas configuradas: {ToolCount}", tools.Count);
    }

    public async Task<string> SendMessageAsync(
        string message,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock MCP: Processando mensagem: {Message}", message);
        
        // Simular delay de processamento
        await Task.Delay(500, cancellationToken);

        return GenerateMockResponse(message);
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string message,
        List<ChatMessage> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock MCP: Stream iniciado para: {Message}", message);
        
        var response = GenerateMockResponse(message);
        var words = response.Split(' ');

        foreach (var word in words)
        {
            await Task.Delay(50, cancellationToken); // Simular streaming lento
            yield return word + " ";
        }

        _logger.LogInformation("Mock MCP: Stream finalizado");
    }

    public async Task<string> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock MCP: Executando tool: {ToolName}", toolName);
        await Task.Delay(100, cancellationToken);
        return "Mock response para: " + toolName;
    }

    private string GenerateMockResponse(string userMessage)
    {
        var messageLower = userMessage.ToLower();

        // Detectar intenção e retornar resposta apropriada
        if (messageLower.Contains("lista") || messageLower.Contains("compra"))
        {
            return "Você tem 3 listas de compras recentes: " +
                   "📋 **Compras Semanais** (17 itens), " +
                   "🛒 **Supermercado X** (8 itens), " +
                   "🌾 **Itens Básicos** (5 itens). " +
                   "Qual você gostaria de ver em detalhes?";
        }

        if (messageLower.Contains("preço") || messageLower.Contains("quanto") || messageLower.Contains("custa"))
        {
            return "Aqui está o histórico de preços dos produtos mais comuns:\n\n" +
                   "🍚 **Arroz** - R$ 5,99 (Supermercado X)\n" +
                   "🫘 **Feijão** - R$ 6,50 (Mercado Y)\n" +
                   "☕ **Café** - R$ 12,90 (Supermercado X)\n" +
                   "🧈 **Óleo** - R$ 8,50 (Mercado Z)\n\n" +
                   "📊 Os preços variaram 5-10% no último mês.";
        }

        if (messageLower.Contains("criar") || messageLower.Contains("nova"))
        {
            return "Perfeito! 🎯 Vou criar uma nova lista para você.\n\n" +
                   "Que itens você gostaria de adicionar? " +
                   "Posso incluir: arroz, feijão, café, óleo, sal, açúcar, etc.\n\n" +
                   "Ou prefere criar uma lista com tema específico (compras semanais, itens básicos)?";
        }

        if (messageLower.Contains("quanto") && messageLower.Contains("gast"))
        {
            return "📊 **Resumo de Gastos - Último Mês**\n\n" +
                   "Total gasto: **R$ 245,80**\n" +
                   "Número de compras: 8\n" +
                   "Ticket médio: R$ 30,73\n\n" +
                   "**Categorias mais caras:**\n" +
                   "🥬 Alimentos frescos - R$ 95,00\n" +
                   "🛒 Alimentos básicos - R$ 120,00\n" +
                   "🧹 Higiene e limpeza - R$ 30,80";
        }

        // Resposta padrão
        return "Olá! 👋 Sou seu assistente de compras inteligente. Posso ajudá-lo com:\n\n" +
               "✨ **Minhas listas** - Veja suas listas de compras\n" +
               "💰 **Histórico de preços** - Consulte preços de produtos\n" +
               "📝 **Criar lista** - Crie novas listas de forma inteligente\n" +
               "📊 **Gastos** - Análise de despesas\n\n" +
               "Como posso ajudá-lo hoje?";
    }
}
