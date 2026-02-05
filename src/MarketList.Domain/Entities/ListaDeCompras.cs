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
    
    // Navegação
    public virtual ICollection<ItemListaDeCompras> Itens { get; set; } = new List<ItemListaDeCompras>();
}
