using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class EmpresaRepository : Repository<Empresa>, IEmpresaRepository
{
    public EmpresaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Empresa?> FindByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Empresa>()
            .FirstOrDefaultAsync(e => e.Nome.ToLower() == nome.ToLower(), cancellationToken);
    }
}
