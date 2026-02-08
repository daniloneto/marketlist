using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

/// <summary>
/// Controller para gerenciar conversas com assistente de compras
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatAssistantService _chatAssistantService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatAssistantService chatAssistantService,
        ILogger<ChatController> logger)
    {
        _chatAssistantService = chatAssistantService;
        _logger = logger;
    }

    /// <summary>
    /// Envia uma mensagem para o assistente e retorna resposta completa
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Obter usuário do contexto autenticado
            var userId = "user-placeholder";

            _logger.LogInformation("Recebido mensagem do chat: {Message}", request.Message);

            var response = await _chatAssistantService.ProcessMessageAsync(
                userId,
                request.Message,
                request.ConversationHistory,
                cancellationToken);

            return Ok(new
            {
                message = response,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem de chat");
            return StatusCode(500, new { error = "Erro ao processar mensagem" });
        }
    }

    /// <summary>
    /// Envia mensagem e retorna resposta via Server-Sent Events (SSE) para streaming
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessage(
        [FromBody] ChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            // TODO: Obter usuário do contexto autenticado
            var userId = "user-placeholder";

            _logger.LogInformation("Iniciando stream de chat");

            await foreach (var chunk in _chatAssistantService.StreamProcessMessageAsync(
                userId,
                request.Message,
                request.ConversationHistory,
                cancellationToken))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer stream de chat");
        }
    }

    /// <summary>
    /// Retorna lista de ferramentas disponíveis
    /// </summary>
    [HttpGet("tools")]
    public IActionResult GetTools()
    {
        try
        {
            var tools = _chatAssistantService.GetAvailableTools();
            return Ok(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter ferramentas");
            return StatusCode(500, new { error = "Erro ao obter ferramentas" });
        }
    }
}
