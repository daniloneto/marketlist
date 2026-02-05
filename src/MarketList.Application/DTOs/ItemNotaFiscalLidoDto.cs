using MarketList.Domain.Enums;

namespace MarketList.Application.DTOs;

/// <summary>
/// Representa um item lido de uma nota fiscal
/// </summary>
public record ItemNotaFiscalLidoDto(
    string NomeProduto,
    string? CodigoLoja,
    decimal Quantidade,
    UnidadeDeMedida UnidadeDeMedida,
    decimal PrecoUnitario,
    decimal PrecoTotal,
    string? TextoOriginal
);
