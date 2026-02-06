namespace MarketList.Application.DTOs;

public record HistoricoPrecoDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    decimal PrecoUnitario,
    DateTime DataConsulta,
    string? FontePreco,
    Guid? EmpresaId,
    string? EmpresaNome
);

public record HistoricoPrecoCreateDto(
    Guid ProdutoId,
    decimal PrecoUnitario,
    string? FontePreco
);
