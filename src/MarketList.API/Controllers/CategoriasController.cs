using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    /// <summary>
    /// Lista todas as categorias
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<CategoriaDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!PaginacaoHelper.TryValidar(pageNumber, pageSize, out var erro))
            return BadRequest(new { error = erro });

        var categorias = await _categoriaService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        return Ok(categorias);
    }

    /// <summary>
    /// Obtém uma categoria pelo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoriaDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var categoria = await _categoriaService.GetByIdAsync(id, cancellationToken);
        if (categoria == null)
            return NotFound();

        return Ok(categoria);
    }

    /// <summary>
    /// Cria uma nova categoria
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Create([FromBody] CategoriaCreateDto dto, CancellationToken cancellationToken)
    {
        var categoria = await _categoriaService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
    }

    /// <summary>
    /// Atualiza uma categoria
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoriaDto>> Update(Guid id, [FromBody] CategoriaUpdateDto dto, CancellationToken cancellationToken)
    {
        var categoria = await _categoriaService.UpdateAsync(id, dto, cancellationToken);
        if (categoria == null)
            return NotFound();

        return Ok(categoria);
    }

    /// <summary>
    /// Remove uma categoria
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _categoriaService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
