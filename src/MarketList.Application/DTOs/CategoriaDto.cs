namespace MarketList.Application.DTOs;

public record CategoriaDto(
    Guid Id,
    string Nome,
    string? Descricao,
    DateTime CreatedAt,
    int QuantidadeProdutos
);

public record CategoriaCreateDto(
    string Nome,
    string? Descricao
);

public record CategoriaUpdateDto(
    string Nome,
    string? Descricao
);
