namespace MarketList.Domain.Entities;

/// <summary>
/// Representa um sinônimo ou nome alternativo para um produto.
/// Usado para mapear nomes "sujos" vindos de importações para produtos corretos.
/// </summary>
public class SinonimoProduto : BaseEntity
{
    /// <summary>
    /// Texto original (raw) vindo da fonte de dados
    /// </summary>
    public string TextoOriginal { get; set; } = string.Empty;
    
    /// <summary>
    /// Versão normalizada do texto (uppercase, sem acentos, sem pontuação)
    /// </summary>
    public string TextoNormalizado { get; set; } = string.Empty;
    
    /// <summary>
    /// Fonte de onde veio esse sinônimo (ex: "NotaFiscal", "ListaSimples")
    /// </summary>
    public string? FonteOrigem { get; set; }
    
    // Relacionamentos
    public Guid ProdutoId { get; set; }
    public virtual Produto Produto { get; set; } = null!;
}
