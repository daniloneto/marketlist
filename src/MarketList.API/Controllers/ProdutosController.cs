using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtoService;
    private readonly IHistoricoPrecoService _historicoPrecoService;

    public ProdutosController(IProdutoService produtoService, IHistoricoPrecoService historicoPrecoService)
    {
        _produtoService = produtoService;
        _historicoPrecoService = historicoPrecoService;
    }

    /// <summary>
    /// Lista todos os produtos
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetAll(CancellationToken cancellationToken)
    {
        var produtos = await _produtoService.GetAllAsync(cancellationToken);
        return Ok(produtos);
    }

    /// <summary>
    /// Obtém um produto pelo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProdutoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var produto = await _produtoService.GetByIdAsync(id, cancellationToken);
        if (produto == null)
            return NotFound();

        return Ok(produto);
    }

    /// <summary>
    /// Lista produtos por categoria
    /// </summary>
    [HttpGet("categoria/{categoriaId:guid}")]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetByCategoria(Guid categoriaId, CancellationToken cancellationToken)
    {
        var produtos = await _produtoService.GetByCategoriaAsync(categoriaId, cancellationToken);
        return Ok(produtos);
    }

    /// <summary>
    /// Cria um novo produto
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProdutoDto>> Create([FromBody] ProdutoCreateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var produto = await _produtoService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = produto.Id }, produto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Atualiza um produto
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProdutoDto>> Update(Guid id, [FromBody] ProdutoUpdateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var produto = await _produtoService.UpdateAsync(id, dto, cancellationToken);
            if (produto == null)
                return NotFound();

            return Ok(produto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove um produto
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _produtoService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Obtém o histórico de preços de um produto
    /// </summary>
    [HttpGet("{id:guid}/historico-precos")]
    public async Task<ActionResult<IEnumerable<HistoricoPrecoDto>>> GetHistoricoPrecos(Guid id, CancellationToken cancellationToken)
    {
        var historico = await _historicoPrecoService.GetByProdutoAsync(id, cancellationToken);
        return Ok(historico);
    }
}
