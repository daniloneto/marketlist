using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketList.Infrastructure.Services;

/// <summary>
/// Cliente MCP agnóstico que suporta múltiplos backends
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClientService> _logger;
    private readonly McpClientOptions _options;
    private List<ToolDefinition> _tools = [];

    public McpClientService(
        HttpClient httpClient,
        IOptions<McpClientOptions> options,
        ILogger<McpClientService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public void SetTools(List<ToolDefinition> tools)
    {
        _tools = tools;
        _logger.LogInformation("Ferramentas MCP configuradas: {ToolCount}", tools.Count);
    }

    public async Task<string> SendMessageAsync(
        string message,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildRequest(message, conversationHistory);
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_options.Endpoint, content, cancellationToken);

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("Resposta MCP recebida com sucesso");
            return ParseResponse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comunicar com servidor MCP");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string message,
        List<ChatMessage> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(message, conversationHistory);
        var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var streamContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Usando stream para SSE
        var streamUrl = _options.Endpoint.Replace("/message", "/stream");
        using var response = await _httpClient.PostAsync(streamUrl, streamContent, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data != "[DONE]")
                {
                    yield return data;
                }
            }
        }

        _logger.LogInformation("Stream MCP finalizado");
    }

    public async Task<string> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executando tool: {ToolName} com parâmetros: {@Parameters}", toolName, parameters);
        // Esta será implementada pelo ChatAssistantService
        throw new NotImplementedException("ExecuteToolAsync deve ser implementado pelo ChatAssistantService");
    }

    private McpRequest BuildRequest(string message, List<ChatMessage> conversationHistory)
    {
        var messages = conversationHistory.ConvertAll(m => new { m.Role, m.Content });
        messages.Add(new { Role = "user", Content = message });

        return new McpRequest
        {
            Model = _options.Model,
            Messages = messages.Cast<object>().ToList(),
            Tools = _tools.ConvertAll(t => new
            {
                Type = "function",
                Function = new
                {
                    t.Name,
                    t.Description,
                    Parameters = new
                    {
                        Type = "object",
                        Properties = t.Parameters.ToDictionary(p => p.Key, p => new
                        {
                            p.Value.Type,
                            p.Value.Description
                        }),
                        Required = t.Parameters.Where(p => p.Value.Required).Select(p => p.Key).ToList()
                    }
                }
            }).Cast<object>().ToList(),
            Temperature = _options.Temperature,
            MaxTokens = _options.MaxTokens
        };
    }

    private string ParseResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Trata diferentes formatos de resposta
            if (root.TryGetProperty("choices"u8, out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("message"u8, out var message))
                {
                    if (message.TryGetProperty("content"u8, out var content))
                    {
                        return content.GetString() ?? "";
                    }
                }
            }

            if (root.TryGetProperty("response"u8, out var response))
            {
                return response.GetString() ?? "";
            }

            return responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }
}

/// <summary>
/// Configurações do cliente MCP
/// </summary>
public class McpClientOptions
{
    public required string Provider { get; set; } // "ollama", "openai", "anthropic"
    public required string Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "mistral";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2048;
}

/// <summary>
/// Estrutura de request para MCP
/// </summary>
internal class McpRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("messages")]
    public required List<object> Messages { get; set; }

    [JsonPropertyName("tools")]
    public List<object> Tools { get; set; } = [];

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }
}
