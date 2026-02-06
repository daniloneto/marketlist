namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço para normalização de textos (nomes de produtos, categorias, etc.)
/// </summary>
public interface ITextoNormalizacaoService
{
    /// <summary>
    /// Normaliza um texto: uppercase, remove acentos, pontuação e espaços extras
    /// </summary>
    string Normalizar(string texto);
}
