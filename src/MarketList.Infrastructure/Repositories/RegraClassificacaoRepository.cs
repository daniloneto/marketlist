using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class RegraClassificacaoRepository : Repository<RegraClassificacaoCategoria>, IRegraClassificacaoRepository
{
    public RegraClassificacaoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RegraClassificacaoCategoria>> GetRegrasOrdenadasAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Categoria)
            .OrderByDescending(r => r.Prioridade)
            .ThenByDescending(r => r.ContagemUsos)
            .ToListAsync(cancellationToken);
    }

    public async Task IncrementarContagemAsync(Guid regraId, CancellationToken cancellationToken = default)
    {
        var regra = await _dbSet.FindAsync([regraId], cancellationToken);
        if (regra != null)
        {
            regra.ContagemUsos++;
            _dbSet.Update(regra);
        }
    }
}
