using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class ListaDeComprasRepository : Repository<ListaDeCompras>, IListaDeComprasRepository
{
    public ListaDeComprasRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ListaDeCompras>> GetByUsuarioIdAsync(string usuarioId, int limit = 10, CancellationToken cancellationToken = default)
    {
        // TODO: Adicionar UserId às entidades se necessário
        // Por enquanto retorna as N listas mais recentes
        return await _context.Set<ListaDeCompras>()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ListaDeCompras?> GetWithItensAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ListaDeCompras>()
            .Include(l => l.Itens)
            .ThenInclude(i => i.Produto)
            .ThenInclude(p => p.Categoria)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }
}
