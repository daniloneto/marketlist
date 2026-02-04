namespace MarketList.Application.DTOs;

public record ProdutoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    string? Unidade,
    Guid CategoriaId,
    string CategoriaNome,
    decimal? UltimoPreco,
    DateTime CreatedAt
);

public record ProdutoCreateDto(
    string Nome,
    string? Descricao,
    string? Unidade,
    Guid CategoriaId
);

public record ProdutoUpdateDto(
    string Nome,
    string? Descricao,
    string? Unidade,
    Guid CategoriaId
);

public record ProdutoResumoDto(
    Guid Id,
    string Nome,
    string? Unidade
);
