namespace MarketList.Domain.Enums;

/// <summary>
/// Define o tipo de entrada/origem da lista de compras
/// </summary>
public enum TipoEntrada
{
    /// <summary>
    /// Lista simples - texto livre com nomes de produtos
    /// </summary>
    ListaSimples = 0,
    
    /// <summary>
    /// Nota fiscal - texto estruturado com preços e quantidades
    /// </summary>
    NotaFiscal = 1
}
