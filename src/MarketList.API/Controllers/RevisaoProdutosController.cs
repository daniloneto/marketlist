using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.API.Helpers;
using MarketList.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/revisao-produtos")]
public class RevisaoProdutosController : ControllerBase
{
    private readonly IProdutoAprovacaoService _aprovacaoService;
    private readonly IProdutoRepository _produtoRepository;

    public RevisaoProdutosController(
        IProdutoAprovacaoService aprovacaoService,
        IProdutoRepository produtoRepository)
    {
        _aprovacaoService = aprovacaoService;
        _produtoRepository = produtoRepository;
    }

    /// <summary>
    /// Lista todos os produtos pendentes de revisão (nome ou categoria)
    /// </summary>
    [HttpGet("pendentes")]
    public async Task<ActionResult<PagedResultDto<ProdutoPendenteDto>>> GetPendentes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!PaginacaoHelper.TryValidar(pageNumber, pageSize, out var erro))
            return BadRequest(new { error = erro });

        var produtos = await _aprovacaoService.ListarPendentesRevisaoAsync(pageNumber, pageSize, cancellationToken);
        return Ok(produtos);
    }

    /// <summary>
    /// Aprova um produto com correções
    /// </summary>
    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, [FromBody] ProdutoAprovacaoDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _aprovacaoService.AprovarComCorrecaoAsync(id, dto, cancellationToken);
            return Ok(new { message = "Produto aprovado com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Vincula um produto provisório a um produto existente (merge)
    /// </summary>
    [HttpPost("{idOrigem:guid}/vincular/{idDestino:guid}")]
    public async Task<IActionResult> Vincular(Guid idOrigem, Guid idDestino, CancellationToken cancellationToken)
    {
        try
        {
            await _aprovacaoService.VincularProdutosAsync(idOrigem, idDestino, cancellationToken);
            return Ok(new { message = "Produtos vinculados com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Busca produtos similares para sugerir vinculação
    /// </summary>
    [HttpGet("{id:guid}/similares")]
    public async Task<ActionResult<IEnumerable<ProdutoResumoDto>>> GetSimilares(Guid id, CancellationToken cancellationToken)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, cancellationToken);
        if (produto == null)
            return NotFound();

        var similares = await _produtoRepository.FindSimilarByNameAsync(produto.Nome, 10, cancellationToken);
        var resultado = similares
            .Where(p => p.Id != id)
            .Select(p => new ProdutoResumoDto(p.Id, p.Nome, p.Unidade));

        return Ok(resultado);
    }
}
