using MarketList.Application.Interfaces;
using MarketList.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManutencaoController : ControllerBase
{
    private readonly ISinonimoRepository _sinonimoRepository;
    private readonly ITextoNormalizacaoService _normalizacaoService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ManutencaoController> _logger;

    public ManutencaoController(
        ISinonimoRepository sinonimoRepository,
        ITextoNormalizacaoService normalizacaoService,
        IUnitOfWork unitOfWork,
        ILogger<ManutencaoController> logger)
    {
        _sinonimoRepository = sinonimoRepository;
        _normalizacaoService = normalizacaoService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Re-normaliza todos os sinônimos usando o código atual de normalização
    /// </summary>
    [HttpPost("renormalizar-sinonimos")]
    public async Task<IActionResult> RenormalizarSinonimos(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Iniciando re-normalização de sinônimos...");

            var sinonimos = (await _sinonimoRepository.GetAllAsync(cancellationToken)).ToList();
            var contador = 0;
            var alterados = 0;

            foreach (var sinonimo in sinonimos)
            {
                contador++;
                var textoNormalizadoAntigo = sinonimo.TextoNormalizado;
                var textoNormalizadoNovo = _normalizacaoService.Normalizar(sinonimo.TextoOriginal);

                if (textoNormalizadoAntigo != textoNormalizadoNovo)
                {
                    sinonimo.TextoNormalizado = textoNormalizadoNovo;
                    await _sinonimoRepository.UpdateAsync(sinonimo, cancellationToken);
                    alterados++;

                    _logger.LogInformation("Sinônimo {Id} atualizado: '{Antigo}' -> '{Novo}'", 
                        sinonimo.Id, textoNormalizadoAntigo, textoNormalizadoNovo);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Re-normalização concluída: {Total} sinônimos processados, {Alterados} alterados", 
                contador, alterados);

            return Ok(new
            {
                message = "Re-normalização concluída com sucesso",
                totalProcessados = contador,
                totalAlterados = alterados
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao re-normalizar sinônimos");
            return StatusCode(500, new { error = "Erro ao re-normalizar sinônimos", details = ex.Message });
        }
    }
}
