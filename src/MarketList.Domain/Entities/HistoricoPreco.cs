namespace MarketList.Domain.Entities;

public class HistoricoPreco : BaseEntity
{
    public Guid ProdutoId { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime DataConsulta { get; set; }
    public string? FontePreco { get; set; } // Identificação da API/fonte
    
    // Navegação
    public virtual Produto Produto { get; set; } = null!;
}
