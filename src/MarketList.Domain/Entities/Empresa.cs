namespace MarketList.Domain.Entities;

public class Empresa : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    
    // Navegação
    public virtual ICollection<ListaDeCompras> Listas { get; set; } = new List<ListaDeCompras>();
    public virtual ICollection<HistoricoPreco> HistoricoPrecos { get; set; } = new List<HistoricoPreco>();
}
