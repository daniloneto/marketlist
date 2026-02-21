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
