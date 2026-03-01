using System.Security.Claims;
using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrcamentosController : ControllerBase
{
    private readonly IOrcamentoCategoriaService _orcamentoService;

    public OrcamentosController(IOrcamentoCategoriaService orcamentoService)
    {
        _orcamentoService = orcamentoService;
    }

    [HttpPost]
    public async Task<ActionResult<OrcamentoCategoriaDto>> CreateOrUpdate(
        [FromBody] CriarOrcamentoCategoriaRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(request.PeriodoTipo))
        {
            return BadRequest(new { error = "PeriodoTipo invalido." });
        }

        try
        {
            var result = await _orcamentoService.CriarOuAtualizarAsync(usuarioId.Value, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrcamentoCategoriaDto>>> ListarPorPeriodo(
        [FromQuery] PeriodoOrcamentoTipo periodoTipo,
        [FromQuery] string? periodoRef,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(periodoTipo))
        {
            return BadRequest(new { error = "PeriodoTipo invalido." });
        }

        var result = await _orcamentoService.ListarPorPeriodoAsync(
            usuarioId.Value,
            periodoTipo,
            periodoRef,
            cancellationToken);

        return Ok(result);
    }


    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardFinanceiroResponseDto>> ObterDashboardFinanceiro(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? categoriaId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] bool somenteComOrcamento = false,
        [FromQuery] bool somenteComGasto = false,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _orcamentoService.ObterDashboardFinanceiroAsync(
                usuarioId.Value,
                new DashboardFinanceiroQueryDto(
                    year,
                    month,
                    categoriaId,
                    dataInicio,
                    dataFim,
                    somenteComOrcamento,
                    somenteComGasto),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
