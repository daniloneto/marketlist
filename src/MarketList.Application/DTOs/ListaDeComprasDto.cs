using MarketList.Domain.Enums;

namespace MarketList.Application.DTOs;

public record ListaDeComprasDto(
    Guid Id,
    string Nome,
    string? TextoOriginal,
    StatusLista Status,
    DateTime CreatedAt,
    DateTime? ProcessadoEm,
    string? ErroProcessamento,
    int QuantidadeItens,
    decimal? ValorTotal
);

public record ListaDeComprasDetalhadaDto(
    Guid Id,
    string Nome,
    string? TextoOriginal,
    StatusLista Status,
    DateTime CreatedAt,
    DateTime? ProcessadoEm,
    string? ErroProcessamento,
    List<ItemListaDeComprasDto> Itens
);

public record ListaDeComprasCreateDto(
    string Nome,
    string TextoOriginal
);

public record ListaDeComprasUpdateDto(
    string Nome
);
