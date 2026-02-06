using MarketList.Domain.Entities;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Resultado da resolução de um produto
/// </summary>
public class ProdutoResolucaoResultado
{
    public Produto Produto { get; set; } = null!;
    public bool FoiCriado { get; set; }
    public OrigemResolucao Origem { get; set; }
}

public enum OrigemResolucao
{
    CodigoLoja,      // Encontrado pelo código da loja
    Sinonimo,        // Encontrado por sinônimo
    NomeExato,       // Encontrado por nome exato
    Criado           // Produto foi criado agora
}

/// <summary>
/// Serviço para resolução de produtos (matching ou criação)
/// </summary>
public interface IProdutoResolverService
{
    /// <summary>
    /// Resolve um produto: busca existente ou cria novo
    /// </summary>
    Task<ProdutoResolucaoResultado> ResolverProdutoAsync(
        string nomeOriginal,
        string? codigoLoja,
        Guid categoriaId,
        CancellationToken cancellationToken = default);
}
