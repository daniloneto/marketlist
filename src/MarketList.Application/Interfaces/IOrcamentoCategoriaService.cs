using MarketList.Application.DTOs;
using MarketList.Domain.Enums;

namespace MarketList.Application.Interfaces;

public interface IOrcamentoCategoriaService
{
    Task<OrcamentoCategoriaDto> CriarOuAtualizarAsync(
        Guid usuarioId,
        CriarOrcamentoCategoriaRequest request,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OrcamentoCategoriaDto>> ListarPorPeriodoAsync(
        Guid usuarioId,
        PeriodoOrcamentoTipo periodoTipo,
        string? periodoReferencia,
        CancellationToken cancellationToken = default);

    Task<ResumoOrcamentoListaDto?> ObterResumoParaListaAsync(
        Guid usuarioId,
        Guid listaId,
        PeriodoOrcamentoTipo periodoTipo = PeriodoOrcamentoTipo.Mensal,
        CancellationToken cancellationToken = default);

    Task<DashboardFinanceiroResponseDto> ObterDashboardFinanceiroAsync(
        Guid usuarioId,
        DashboardFinanceiroQueryDto query,
        CancellationToken cancellationToken = default);
}
