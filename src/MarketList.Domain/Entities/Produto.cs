namespace MarketList.Domain.Entities;

public class Produto : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Unidade { get; set; } // kg, un, L, etc. (mantido para compatibilidade)
    public string? CodigoLoja { get; set; } // Código da loja (ex: AR004808)
    
    // Relacionamentos
    public Guid CategoriaId { get; set; }
    public virtual Categoria Categoria { get; set; } = null!;
    
    // Navegação
    public virtual ICollection<HistoricoPreco> HistoricoPrecos { get; set; } = new List<HistoricoPreco>();
    public virtual ICollection<ItemListaDeCompras> ItensLista { get; set; } = new List<ItemListaDeCompras>();
}
