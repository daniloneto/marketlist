using System.Security.Claims;
using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListasDeComprasController : ControllerBase
{
    private readonly IListaDeComprasService _listaService;
    private readonly IOrcamentoCategoriaService _orcamentoService;

    public ListasDeComprasController(
        IListaDeComprasService listaService,
        IOrcamentoCategoriaService orcamentoService)
    {
        _listaService = listaService;
        _orcamentoService = orcamentoService;
    }

    /// <summary>
    /// Lista todas as listas de compras
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListaDeComprasDto>>> GetAll(CancellationToken cancellationToken)
    {
        var listas = await _listaService.GetAllAsync(cancellationToken);
        return Ok(listas);
    }

    /// <summary>
    /// Obtém uma lista de compras pelo ID (com itens)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListaDeComprasDetalhadaDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lista = await _listaService.GetByIdAsync(id, cancellationToken);
        if (lista == null)
            return NotFound();

        return Ok(lista);
    }

    /// <summary>
    /// Cria uma nova lista de compras a partir de texto
    /// O processamento é feito de forma assíncrona via Hangfire
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ListaDeComprasDto>> Create([FromBody] ListaDeComprasCreateDto dto, CancellationToken cancellationToken)
    {
        var lista = await _listaService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = lista.Id }, lista);
    }

    /// <summary>
    /// Atualiza o nome de uma lista
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ListaDeComprasDto>> Update(Guid id, [FromBody] ListaDeComprasUpdateDto dto, CancellationToken cancellationToken)
    {
        var lista = await _listaService.UpdateAsync(id, dto, cancellationToken);
        if (lista == null)
            return NotFound();

        return Ok(lista);
    }

    /// <summary>
    /// Remove uma lista de compras
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _listaService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Adiciona um item manualmente à lista
    /// </summary>
    [HttpPost("{id:guid}/itens")]
    public async Task<ActionResult<ItemListaDeComprasDto>> AddItem(Guid id, [FromBody] ItemListaDeComprasCreateDto dto, CancellationToken cancellationToken)
    {
        var item = await _listaService.AddItemAsync(id, dto, cancellationToken);
        if (item == null)
            return NotFound();

        return CreatedAtAction(nameof(GetById), new { id }, item);
    }

    /// <summary>
    /// Atualiza um item da lista
    /// </summary>
    [HttpPut("{id:guid}/itens/{itemId:guid}")]
    public async Task<ActionResult<ItemListaDeComprasDto>> UpdateItem(Guid id, Guid itemId, [FromBody] ItemListaDeComprasUpdateDto dto, CancellationToken cancellationToken)
    {
        var item = await _listaService.UpdateItemAsync(id, itemId, dto, cancellationToken);
        if (item == null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Remove um item da lista
    /// </summary>
    [HttpDelete("{id:guid}/itens/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var removed = await _listaService.RemoveItemAsync(id, itemId, cancellationToken);
        if (!removed)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id:guid}/resumo-orcamento")]
    public async Task<ActionResult<ResumoOrcamentoListaDto>> ObterResumoOrcamento(
        Guid id,
        [FromQuery] PeriodoOrcamentoTipo periodoTipo = PeriodoOrcamentoTipo.Mensal,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var resumo = await _orcamentoService.ObterResumoParaListaAsync(
            usuarioId.Value,
            id,
            periodoTipo,
            cancellationToken);
        if (resumo is null)
        {
            return NotFound();
        }

        return Ok(resumo);
    }

    private Guid? ObterUsuarioId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var usuarioId) ? usuarioId : null;
    }
}
