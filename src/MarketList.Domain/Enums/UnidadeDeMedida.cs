namespace MarketList.Domain.Enums;

/// <summary>
/// Unidades de medida para produtos
/// </summary>
public enum UnidadeDeMedida
{
    /// <summary>
    /// Unidade (UND, UND9, UN)
    /// </summary>
    Unidade = 0,
    
    /// <summary>
    /// Quilograma (KG, KG9)
    /// </summary>
    Quilograma = 1,
    
    /// <summary>
    /// Pacote (PCT, PCT1, PCT9)
    /// </summary>
    Pacote = 2,
    
    /// <summary>
    /// Bandeja (BDJ, BDJ9)
    /// </summary>
    Bandeja = 3,
    
    /// <summary>
    /// Maço (MCO, MCO1)
    /// </summary>
    Maco = 4,
    
    /// <summary>
    /// Frasco (FRC, FRC9)
    /// </summary>
    Frasco = 5,
    
    /// <summary>
    /// Litro (L)
    /// </summary>
    Litro = 6,
    
    /// <summary>
    /// Grama (G)
    /// </summary>
    Grama = 7,
    
    /// <summary>
    /// Caixa (CX)
    /// </summary>
    Caixa = 8
}
