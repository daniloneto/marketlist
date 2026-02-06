using FuzzySharp;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class ProdutoRepository : Repository<Produto>, IProdutoRepository
{
    public ProdutoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Produto?> FindByCodigoLojaAsync(string codigoLoja, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoLoja))
            return null;

        return await _dbSet
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(
                p => p.CodigoLoja != null && p.CodigoLoja == codigoLoja, 
                cancellationToken);
    }

    public async Task<Produto?> FindByNomeNormalizadoAsync(string nomeNormalizado, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeNormalizado))
            return null;

        return await _dbSet
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(
                p => p.NomeNormalizado == nomeNormalizado, 
                cancellationToken);
    }

    public async Task<IEnumerable<Produto>> FindSimilarByNameAsync(string nome, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Enumerable.Empty<Produto>();

        // Carrega produtos do banco (poderia ser otimizado com paginação ou filtro inicial)
        var produtos = await _dbSet
            .Include(p => p.Categoria)
            .ToListAsync(cancellationToken);

        // Calcula similaridade usando FuzzySharp
        var produtosComScore = produtos
            .Select(p => new
            {
                Produto = p,
                Score = Fuzz.Ratio(nome.ToUpperInvariant(), (p.NomeNormalizado ?? p.Nome).ToUpperInvariant())
            })
            .Where(x => x.Score >= 80) // Threshold de 80% de similaridade
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Produto);

        return produtosComScore;
    }

    public async Task<IEnumerable<Produto>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Categoria)
            .Where(p => p.PrecisaRevisao)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Produto>> GetPendingCategoryReviewAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaPrecisaRevisao)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MigrateProdutoAsync(Guid fromId, Guid toId, CancellationToken cancellationToken = default)
    {
        // Migra histórico de preços
        await _context.HistoricoPrecos
            .Where(h => h.ProdutoId == fromId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(h => h.ProdutoId, toId),
                cancellationToken);

        // Migra itens de lista
        await _context.ItensListaDeCompras
            .Where(i => i.ProdutoId == fromId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(i => i.ProdutoId, toId),
                cancellationToken);
    }
}
