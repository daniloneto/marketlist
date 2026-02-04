namespace MarketList.Application.DTOs;

public record PrecoExternoDto(
    string NomeProduto,
    decimal? Preco,
    string? Fonte,
    bool Sucesso,
    string? Erro
);

public record ItemAnalisadoDto(
    string TextoOriginal,
    string NomeProduto,
    decimal Quantidade,
    string? Unidade
);
