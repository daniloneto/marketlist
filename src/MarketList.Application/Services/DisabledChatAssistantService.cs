using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Application.Services;

/// <summary>
/// No-op implementation used when chatbot feature is disabled.
/// Returns friendly error or empty responses and avoids any external calls.
/// </summary>
public class DisabledChatAssistantService : IChatAssistantService
{
    private readonly ILogger<DisabledChatAssistantService> _logger;

    public DisabledChatAssistantService(ILogger<DisabledChatAssistantService> logger)
    {
        _logger = logger;
    }

    public Task<string> ProcessMessageAsync(string userId, string userMessage, List<ChatMessage> conversationHistory, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Chatbot feature is disabled. Blocking request for user {UserId}.", userId);
        throw new InvalidOperationException("Chatbot desativado");
    }

    public async IAsyncEnumerable<string> StreamProcessMessageAsync(string userId, string userMessage, List<ChatMessage> conversationHistory, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Chatbot feature is disabled. Blocking streaming for user {UserId}.", userId);
        yield break;
    }

    public List<ToolDefinition> GetAvailableTools()
    {
        return new List<ToolDefinition>();
    }
}
