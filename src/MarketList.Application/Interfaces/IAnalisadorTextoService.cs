using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço responsável por analisar o texto bruto e identificar produtos
/// </summary>
public interface IAnalisadorTextoService
{
    /// <summary>
    /// Analisa o texto bruto e extrai os itens da lista
    /// </summary>
    List<ItemAnalisadoDto> AnalisarTexto(string textoOriginal);
    
    /// <summary>
    /// Detecta a categoria provável de um produto pelo nome
    /// </summary>
    string DetectarCategoria(string nomeProduto);
}
