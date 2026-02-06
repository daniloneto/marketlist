using MarketList.Domain.Entities;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Resultado da classificação de categoria
/// </summary>
public class CategoriaClassificacaoResultado
{
    public Guid CategoriaId { get; set; }
    public Confianca Confianca { get; set; }
    public Guid? RegraAplicadaId { get; set; }
}

public enum Confianca
{
    Alta,   // Categoria determinada por regra conhecida
    Baixa   // Categoria padrão (Outros) ou sem certeza
}

/// <summary>
/// Serviço para classificação automática de produtos em categorias
/// </summary>
public interface ICategoriaClassificadorService
{
    /// <summary>
    /// Classifica um produto em uma categoria baseado no nome
    /// </summary>
    Task<CategoriaClassificacaoResultado> ClassificarAsync(
        string nomeProduto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aprende a classificação baseado em correção manual do usuário
    /// Cria regras de classificação para futuros produtos similares
    /// </summary>
    Task AprenderClassificacaoAsync(
        Guid produtoId,
        Guid categoriaId,
        CancellationToken cancellationToken = default);
}
