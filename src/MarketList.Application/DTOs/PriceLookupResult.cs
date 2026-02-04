namespace MarketList.Application.DTOs;

/// <summary>
/// Resultado da consulta de preços externos
/// </summary>
public class PriceLookupResult
{
    /// <summary>
    /// Indica se o preço foi encontrado
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Preço encontrado (menor preço disponível)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Data da consulta ou do preço
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Nome do estabelecimento onde o preço foi encontrado
    /// </summary>
    public string? StoreName { get; set; }
}
