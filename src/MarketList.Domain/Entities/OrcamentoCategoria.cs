using MarketList.Domain.Enums;

namespace MarketList.Domain.Entities;

public class OrcamentoCategoria : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public Guid CategoriaId { get; set; }
    public PeriodoOrcamentoTipo PeriodoTipo { get; set; }
    public string PeriodoReferencia { get; set; } = string.Empty;
    public decimal ValorLimite { get; set; }

    public virtual Categoria Categoria { get; set; } = null!;
    public virtual Usuario? Usuario { get; set; }
}
