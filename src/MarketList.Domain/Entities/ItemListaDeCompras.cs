using MarketList.Domain.Enums;

namespace MarketList.Domain.Entities;

public class ItemListaDeCompras : BaseEntity
{
    public Guid ListaDeComprasId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public UnidadeDeMedida? UnidadeDeMedida { get; set; } // Unidade de medida do item
    public decimal? PrecoUnitario { get; set; } // Último preço conhecido no momento da criação
    public decimal? PrecoTotal { get; set; } // Preço total do item (Quantidade * PrecoUnitario)
    public string? TextoOriginal { get; set; } // Linha original do texto
    public string? RawName { get; set; }
    public string? ResolvedName { get; set; }
    public Guid? ResolvedCategoryId { get; set; }
    public decimal? MatchScore { get; set; }
    public ProductResolutionStatus? ResolutionStatus { get; set; }
    public bool Comprado { get; set; } = false;
    
    // Navegação
    public virtual ListaDeCompras ListaDeCompras { get; set; } = null!;
    public virtual Produto Produto { get; set; } = null!;
    
    // Calculado
    public decimal? SubTotal => PrecoTotal ?? (PrecoUnitario.HasValue ? PrecoUnitario * Quantidade : null);
}
