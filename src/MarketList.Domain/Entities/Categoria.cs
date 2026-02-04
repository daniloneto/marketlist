namespace MarketList.Domain.Entities;

public class Categoria : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    
    // Navegação
    public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}
