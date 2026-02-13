namespace MarketList.Application.DTOs;

/// <summary>
/// DTO retornado pelo crawler da NFC-e contendo o texto estruturado
/// e a data de emissão extraída do HTML da SEFAZ.
/// </summary>
public record NotaFiscalExtraidaDto(
    /// <summary>
    /// Texto estruturado no formato do pipeline (empresa na primeira linha, itens abaixo).
    /// </summary>
    string Texto,

    /// <summary>
    /// Data de emissão da NFC-e extraída do HTML.
    /// Se não encontrada, contém DateTime.UtcNow como fallback.
    /// </summary>
    DateTime DataEmissao
);
