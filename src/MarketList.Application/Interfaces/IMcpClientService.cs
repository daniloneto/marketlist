namespace MarketList.Application.Interfaces;

/// <summary>
/// Interface para comunicação com servidores MCP (Model Context Protocol)
/// Suporta múltiplos backends: Ollama local, OpenAI, Anthropic
/// </summary>
public interface IMcpClientService
{
    /// <summary>
    /// Enviá uma mensagem para o servidor MCP e obtém uma resposta
    /// </summary>
    Task<string> SendMessageAsync(string message, List<ChatMessage> conversationHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia mensagem e retorna stream de tokens para resposta em tiempo real
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(string message, List<ChatMessage> conversationHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Define ferramentas (tools) disponíveis para o LLM
    /// </summary>
    void SetTools(List<ToolDefinition> tools);

    /// <summary>
    /// Executa uma tool chamada pelo LLM e retorna o resultado
    /// </summary>
    Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Representa uma mensagem no histórico de conversa
/// </summary>
public class ChatMessage
{
    public required string Role { get; set; } // "user", "assistant", "system"
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Define uma ferramenta que o LLM pode usar
/// </summary>
public class ToolDefinition
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string, ParameterDefinition> Parameters { get; set; }
}

/// <summary>
/// Define um parâmetro de ferramenta
/// </summary>
public class ParameterDefinition
{
    public required string Type { get; set; } // "string", "number", "boolean", etc
    public required string Description { get; set; }
    public bool Required { get; set; }
}
