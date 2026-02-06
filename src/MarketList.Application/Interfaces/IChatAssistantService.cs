namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço para orquestração do assistente de chat
/// Define as tools disponíveis e gerencia execução
/// </summary>
public interface IChatAssistantService
{
    /// <summary>
    /// Processa mensagem do usuário e retorna resposta do assistente
    /// </summary>
    Task<string> ProcessMessageAsync(
        string userId,
        string userMessage,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa mensagem com streaming de resposta
    /// </summary>
    IAsyncEnumerable<string> StreamProcessMessageAsync(
        string userId,
        string userMessage,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna todas as tools disponíveis
    /// </summary>
    List<ToolDefinition> GetAvailableTools();
}

/// <summary>
/// Request para enviar mensagem
/// </summary>
public class ChatMessageRequest
{
    public required string Message { get; set; }
    public List<ChatMessage> ConversationHistory { get; set; } = [];
}

/// <summary>
/// Response de mensagem
/// </summary>
public class ChatMessageResponse
{
    public required string Message { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Representa uma chamada de tool feita pelo LLM
/// </summary>
public class ToolCall
{
    public required string ToolName { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
    public string? Result { get; set; }
}
