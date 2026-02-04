using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço para consulta de preços externos
/// </summary>
public interface IPriceLookupService
{
    /// <summary>
    /// Obtém o menor preço disponível para um produto em uma localização específica
    /// </summary>
    /// <param name="productNameOrGtin">Nome do produto ou código GTIN</param>
    /// <param name="latitude">Latitude da localização</param>
    /// <param name="longitude">Longitude da localização</param>
    /// <param name="hours">Janela de tempo em horas para buscar preços recentes</param>
    /// <returns>Resultado da consulta de preços</returns>
    Task<PriceLookupResult> GetLatestPriceAsync(
        string productNameOrGtin,
        double latitude,
        double longitude,
        int hours = 24);
}
