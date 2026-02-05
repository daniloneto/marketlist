using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço responsável por ler e interpretar textos de nota fiscal
/// </summary>
public interface ILeitorNotaFiscal
{
    /// <summary>
    /// Lê o texto bruto de uma nota fiscal e extrai os itens
    /// </summary>
    /// <param name="textoBruto">Texto da nota fiscal</param>
    /// <returns>Lista de itens identificados na nota</returns>
    List<ItemNotaFiscalLidoDto> Ler(string textoBruto);
}
