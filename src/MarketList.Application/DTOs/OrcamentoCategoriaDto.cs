using MarketList.Domain.Enums;

namespace MarketList.Application.DTOs;

public record OrcamentoCategoriaDto(
    Guid Id,
    Guid UsuarioId,
    Guid CategoriaId,
    string NomeCategoria,
    PeriodoOrcamentoTipo PeriodoTipo,
    string PeriodoReferencia,
    decimal ValorLimite,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CriarOrcamentoCategoriaRequest(
    Guid CategoriaId,
    PeriodoOrcamentoTipo PeriodoTipo,
    string? PeriodoReferencia,
    decimal ValorLimite
);

public record AtualizarOrcamentoCategoriaRequest(
    decimal ValorLimite
);

public record ResumoOrcamentoListaDto(
    Guid ListaId,
    string PeriodoReferencia,
    PeriodoOrcamentoTipo PeriodoTipo,
    decimal TotalLista,
    int TotalItensSemPreco,
    IReadOnlyList<ItemResumoOrcamentoCategoriaDto> ItensPorCategoria
);

public record ItemResumoOrcamentoCategoriaDto(
    Guid CategoriaId,
    string NomeCategoria,
    decimal TotalEstimado,
    int ItensSemPreco,
    decimal ValorLimite,
    decimal PercentualConsumido,
    StatusConsumoOrcamento Status
);

public record DashboardFinanceiroQueryDto(
    int Year,
    int Month,
    Guid? CategoriaId,
    DateTime? DataInicio,
    DateTime? DataFim,
    bool SomenteComOrcamento,
    bool SomenteComGasto
);

public record DashboardFinanceiroResumoDto(
    decimal TotalBudget,
    decimal TotalSpent,
    decimal TotalRemaining,
    decimal? TotalPercentageUsed
);

public record DashboardFinanceiroCategoriaDto(
    Guid CategoryId,
    string CategoryName,
    decimal? BudgetAmount,
    decimal SpentAmount,
    decimal? RemainingAmount,
    decimal? PercentageUsed
);

public record DashboardFinanceiroResponseDto(
    int Year,
    int Month,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DashboardFinanceiroResumoDto Summary,
    IReadOnlyList<DashboardFinanceiroCategoriaDto> Categories
);
