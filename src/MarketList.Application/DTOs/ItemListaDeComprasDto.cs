namespace MarketList.Application.DTOs;

public record ItemListaDeComprasDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    string? ProdutoUnidade,
    decimal Quantidade,
    decimal? PrecoUnitario,
    decimal? SubTotal,
    string? TextoOriginal,
    bool Comprado
);

public record ItemListaDeComprasCreateDto(
    Guid ProdutoId,
    decimal Quantidade
);

public record ItemListaDeComprasUpdateDto(
    decimal Quantidade,
    bool Comprado
);
