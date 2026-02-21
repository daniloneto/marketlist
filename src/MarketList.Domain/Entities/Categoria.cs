namespace MarketList.Domain.Entities;

public class Categoria : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    // Navegacao
    public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    public virtual ICollection<OrcamentoCategoria> Orcamentos { get; set; } = new List<OrcamentoCategoria>();
}
