namespace MarketList.Application.DTOs;

/// <summary>
/// DTO que representa um item analisado de uma lista de compras simples (texto livre)
/// </summary>
public record ItemAnalisadoDto(
    string TextoOriginal,
    string NomeProduto,
    decimal Quantidade,
    string? Unidade
);
