namespace MarketList.Infrastructure.Configurations;

public class IntegracoesOptions
{
    // Timeout padrão para integrações externas (em segundos)
    public int TimeoutSegundos { get; set; } = 30;
}
