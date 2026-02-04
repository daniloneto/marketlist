namespace MarketList.Domain.Entities;

public class ItemListaDeCompras : BaseEntity
{
    public Guid ListaDeComprasId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal? PrecoUnitario { get; set; } // Último preço conhecido no momento da criação
    public string? TextoOriginal { get; set; } // Linha original do texto
    public bool Comprado { get; set; } = false;
    
    // Navegação
    public virtual ListaDeCompras ListaDeCompras { get; set; } = null!;
    public virtual Produto Produto { get; set; } = null!;
    
    // Calculado
    public decimal? SubTotal => PrecoUnitario.HasValue ? PrecoUnitario * Quantidade : null;
}
