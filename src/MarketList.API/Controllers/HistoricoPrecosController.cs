using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoricoPrecosController : ControllerBase
{
    private readonly IHistoricoPrecoService _historicoPrecoService;

    public HistoricoPrecosController(IHistoricoPrecoService historicoPrecoService)
    {
        _historicoPrecoService = historicoPrecoService;
    }

    /// <summary>
    /// Lista todo o histórico de preços
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HistoricoPrecoDto>>> GetAll(CancellationToken cancellationToken)
    {
        var historico = await _historicoPrecoService.GetAllAsync(cancellationToken);
        return Ok(historico);
    }

    /// <summary>
    /// Lista histórico de preços por produto
    /// </summary>
    [HttpGet("produto/{produtoId:guid}")]
    public async Task<ActionResult<IEnumerable<HistoricoPrecoDto>>> GetByProduto(Guid produtoId, CancellationToken cancellationToken)
    {
        var historico = await _historicoPrecoService.GetByProdutoAsync(produtoId, cancellationToken);
        return Ok(historico);
    }

    /// <summary>
    /// Obtém o último preço de um produto
    /// </summary>
    [HttpGet("produto/{produtoId:guid}/ultimo")]
    public async Task<ActionResult<HistoricoPrecoDto>> GetUltimoPreco(Guid produtoId, CancellationToken cancellationToken)
    {
        var historico = await _historicoPrecoService.GetUltimoPrecoAsync(produtoId, cancellationToken);
        if (historico == null)
            return NotFound();

        return Ok(historico);
    }

    /// <summary>
    /// Registra um novo preço manualmente
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HistoricoPrecoDto>> Create([FromBody] HistoricoPrecoCreateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var historico = await _historicoPrecoService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetByProduto), new { produtoId = dto.ProdutoId }, historico);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
