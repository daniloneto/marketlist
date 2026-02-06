using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class HistoricoPrecoRepository : Repository<HistoricoPreco>, IHistoricoPrecoRepository
{
    private readonly AppDbContext _context;

    public HistoricoPrecoRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HistoricoPreco>> GetByProdutoIdAsync(Guid produtoId, int days = 90, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        return await _context.Set<HistoricoPreco>()
            .Include(h => h.Empresa)
            .Where(h => h.ProdutoId == produtoId && h.DataConsulta >= startDate)
            .OrderByDescending(h => h.DataConsulta)
            .ToListAsync(cancellationToken);
    }

    public async Task<HistoricoPreco?> GetLatestByProdutoIdAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<HistoricoPreco>()
            .Include(h => h.Empresa)
            .Where(h => h.ProdutoId == produtoId)
            .OrderByDescending(h => h.DataConsulta)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
