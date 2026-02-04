namespace MarketList.Domain.Entities;

public class Produto : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Unidade { get; set; } // kg, un, L, etc.
    
    // Relacionamentos
    public Guid CategoriaId { get; set; }
    public virtual Categoria Categoria { get; set; } = null!;
    
    // Navegação
    public virtual ICollection<HistoricoPreco> HistoricoPrecos { get; set; } = new List<HistoricoPreco>();
    public virtual ICollection<ItemListaDeCompras> ItensLista { get; set; } = new List<ItemListaDeCompras>();
}
