using MarketList.Domain.Enums;

namespace MarketList.Domain.Entities;

public class ListaDeCompras : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? TextoOriginal { get; set; } // Texto bruto enviado pelo usuário
    public TipoEntrada TipoEntrada { get; set; } = TipoEntrada.ListaSimples; // Tipo de origem da lista
    public StatusLista Status { get; set; } = StatusLista.Pendente;
    public DateTime? ProcessadoEm { get; set; }
    public string? ErroProcessamento { get; set; }
    
    /// <summary>
    /// Data da compra / data de emissão da NFC-e.
    /// Preenchida automaticamente quando a lista é importada via QR Code.
    /// </summary>
    public DateTime? DataCompra { get; set; }
    
    // Relacionamento com Empresa (nullable para listas antigas e listas simples)
    public Guid? EmpresaId { get; set; }
    public virtual Empresa? Empresa { get; set; }
    
    // Navegação
    public virtual ICollection<ItemListaDeCompras> Itens { get; set; } = new List<ItemListaDeCompras>();
}
