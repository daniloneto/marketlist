using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
{
    private readonly AppDbContext _context;

    public CategoriaRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Categoria?> FindByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Categoria>()
            .FirstOrDefaultAsync(c => c.Nome.ToLower() == nome.ToLower(), cancellationToken);
    }
}
