using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class SinonimoRepository : Repository<SinonimoProduto>, ISinonimoRepository
{
    public SinonimoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SinonimoProduto?> FindByTextoNormalizadoAsync(string textoNormalizado, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(textoNormalizado))
            return null;

        return await _dbSet
            .Include(s => s.Produto)
                .ThenInclude(p => p.Categoria)
            .FirstOrDefaultAsync(
                s => s.TextoNormalizado == textoNormalizado, 
                cancellationToken);
    }
}
