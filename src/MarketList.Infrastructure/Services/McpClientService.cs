using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketList.Infrastructure.Services;

/// <summary>
/// Cliente MCP para Ollama
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
            var request = BuildOllamaRequest(message, conversationHistory, stream: false);
            var jsonContent = JsonSerializer.Serialize(request);
            
            _logger.LogDebug("Enviando para Ollama: {Request}", jsonContent);
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_options.Endpoint, content, cancellationToken);

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("Resposta Ollama: {Response}", responseBody);
            return ParseOllamaResponse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comunicar com Ollama");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string message,
        List<ChatMessage> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildOllamaRequest(message, conversationHistory, stream: true);
        var jsonContent = JsonSerializer.Serialize(request);

        var streamContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = streamContent
        };
        
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var (text, done) = ParseStreamLine(line);
            
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
            
            if (done) break;
        }

        _logger.LogInformation("Stream Ollama finalizado");
    }
    
    private (string? text, bool done) ParseStreamLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            string? text = null;
            bool done = false;
            
            if (doc.RootElement.TryGetProperty("response", out var responseText))
            {
                text = responseText.GetString();
            }
            
            if (doc.RootElement.TryGetProperty("done", out var doneElement))
            {
                done = doneElement.GetBoolean();
            }
            
            return (text, done);
        }
        catch (JsonException)
        {
            return (null, false);
        }
    }

    public async Task<string> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executando tool: {ToolName} com parâmetros: {@Parameters}", toolName, parameters);
        throw new NotImplementedException("ExecuteToolAsync deve ser implementado pelo ChatAssistantService");
    }

    private object BuildOllamaRequest(string message, List<ChatMessage> conversationHistory, bool stream)
    {
        // Constrói o prompt com o histórico
        var promptBuilder = new StringBuilder();
        
        // Adiciona contexto do sistema com instruções rigorosas
        promptBuilder.AppendLine(@"Você é um assistente de compras do sistema MarketList.

REGRAS CRÍTICAS - SIGA ESTRITAMENTE:
1. Responda APENAS com base nos dados fornecidos na seção ""CONTEXTO DO SISTEMA"".
2. NUNCA invente, crie ou suponha dados que não estejam explicitamente listados no contexto.
3. Se não houver dados no contexto para responder uma pergunta, diga: ""Não há registros disponíveis para essa consulta.""
4. Não adicione itens, produtos, categorias ou valores que não apareçam no contexto.
5. Se o contexto estiver vazio ou com poucos dados, informe isso ao usuário.
6. Seja preciso: use exatamente os nomes, quantidades e valores mostrados no contexto.
7. Quando listar itens, liste APENAS o que aparece no contexto, nada mais.

Lembre-se: você é um assistente que consulta um banco de dados real. Inventar dados seria fornecer informações falsas ao usuário.
");
        promptBuilder.AppendLine();
        
        // Adiciona histórico
        foreach (var msg in conversationHistory)
        {
            var prefix = msg.Role == "user" ? "Usuário" : "Assistente";
            promptBuilder.AppendLine($"{prefix}: {msg.Content}");
        }
        
        // Adiciona mensagem atual
        promptBuilder.AppendLine($"Usuário: {message}");
        promptBuilder.AppendLine("Assistente:");

        return new
        {
            model = _options.Model,
            prompt = promptBuilder.ToString(),
            stream = stream,
            options = new
            {
                temperature = _options.Temperature,
                num_predict = _options.MaxTokens
            }
        };
    }

    private string ParseOllamaResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("response", out var response))
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
    public required string Provider { get; set; }
    public required string Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "mistral";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2048;
    public bool UseMock { get; set; } = false;
}
