using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _empresaService;

    public EmpresasController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    /// <summary>
    /// Lista todas as empresas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpresaDto>>> GetAll(CancellationToken cancellationToken)
    {
        var empresas = await _empresaService.GetAllAsync(cancellationToken);
        return Ok(empresas);
    }

    /// <summary>
    /// Obtém uma empresa pelo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmpresaDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var empresa = await _empresaService.GetByIdAsync(id, cancellationToken);
        if (empresa == null)
            return NotFound();

        return Ok(empresa);
    }

    /// <summary>
    /// Cria uma nova empresa
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmpresaDto>> Create([FromBody] EmpresaCreateDto dto, CancellationToken cancellationToken)
    {
        var empresa = await _empresaService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = empresa.Id }, empresa);
    }

    /// <summary>
    /// Atualiza uma empresa
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmpresaDto>> Update(Guid id, [FromBody] EmpresaUpdateDto dto, CancellationToken cancellationToken)
    {
        var empresa = await _empresaService.UpdateAsync(id, dto, cancellationToken);
        if (empresa == null)
            return NotFound();

        return Ok(empresa);
    }

    /// <summary>
    /// Remove uma empresa
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _empresaService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
