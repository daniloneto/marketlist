namespace MarketList.Domain.Entities;

/// <summary>
/// Regra de classificação automática de produtos em categorias.
/// O sistema aprende essas regras quando o usuário corrige categorias manualmente.
/// </summary>
public class RegraClassificacaoCategoria : BaseEntity
{
    /// <summary>
    /// Termo normalizado que, quando encontrado no nome do produto, sugere essa categoria
    /// </summary>
    public string TermoNormalizado { get; set; } = string.Empty;
    
    /// <summary>
    /// Prioridade da regra (maior = aplicada primeiro). Usado para resolver conflitos.
    /// </summary>
    public int Prioridade { get; set; } = 0;
    
    /// <summary>
    /// Contador de quantas vezes essa regra foi aplicada com sucesso
    /// </summary>
    public int ContagemUsos { get; set; } = 0;
    
    // Relacionamentos
    public Guid CategoriaId { get; set; }
    public virtual Categoria Categoria { get; set; } = null!;
}
