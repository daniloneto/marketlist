using MarketList.Domain.Enums;

namespace MarketList.Application.DTOs;

public record ListaDeComprasDto(
    Guid Id,
    string Nome,
    string? TextoOriginal,
    TipoEntrada TipoEntrada,
    StatusLista Status,
    DateTime CreatedAt,
    DateTime? ProcessadoEm,
    string? ErroProcessamento,
    int QuantidadeItens,
    decimal? ValorTotal,
    Guid? EmpresaId,
    string? EmpresaNome,
    DateTime? DataCompra = null
);

public record ListaDeComprasDetalhadaDto(
    Guid Id,
    string Nome,
    string? TextoOriginal,
    TipoEntrada TipoEntrada,
    StatusLista Status,
    DateTime CreatedAt,
    DateTime? ProcessadoEm,
    string? ErroProcessamento,
    List<ItemListaDeComprasDto> Itens,
    Guid? EmpresaId,
    string? EmpresaNome,
    DateTime? DataCompra = null
);

public record ListaDeComprasCreateDto(
    string Nome,
    string TextoOriginal,
    TipoEntrada TipoEntrada = TipoEntrada.ListaSimples,
    Guid? EmpresaId = null,
    DateTime? DataCompra = null
);

public record ListaDeComprasUpdateDto(
    string Nome
);
