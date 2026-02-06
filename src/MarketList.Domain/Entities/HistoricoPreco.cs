namespace MarketList.Domain.Entities;

public class HistoricoPreco : BaseEntity
{
    public Guid ProdutoId { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime DataConsulta { get; set; }
    public string? FontePreco { get; set; } // Identificação da API/fonte
    
    // Relacionamento com Empresa (nullable para históricos antigos)
    public Guid? EmpresaId { get; set; }
    public virtual Empresa? Empresa { get; set; }
    
    // Navegação
    public virtual Produto Produto { get; set; } = null!;
}
