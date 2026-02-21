using MarketList.Domain.Entities;
using MarketList.Domain.Enums;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Repositories;

public class OrcamentoCategoriaRepository : Repository<OrcamentoCategoria>, IOrcamentoCategoriaRepository
{
    public OrcamentoCategoriaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<OrcamentoCategoria?> GetByCategoriaAndPeriodoAsync(
        Guid usuarioId,
        Guid categoriaId,
        PeriodoOrcamentoTipo periodoTipo,
        string periodoReferencia,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<OrcamentoCategoria>()
            .Include(o => o.Categoria)
            .FirstOrDefaultAsync(
                o => o.UsuarioId == usuarioId
                     && o.CategoriaId == categoriaId
                     && o.PeriodoTipo == periodoTipo
                     && o.PeriodoReferencia == periodoReferencia,
                cancellationToken);
    }

    public async Task<IEnumerable<OrcamentoCategoria>> ListByPeriodoAsync(
        Guid usuarioId,
        PeriodoOrcamentoTipo periodoTipo,
        string periodoReferencia,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<OrcamentoCategoria>()
            .Include(o => o.Categoria)
            .Where(o => o.UsuarioId == usuarioId
                        && o.PeriodoTipo == periodoTipo
                        && o.PeriodoReferencia == periodoReferencia)
            .OrderBy(o => o.Categoria.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrcamentoCategoria> UpsertAsync(
        OrcamentoCategoria entity,
        CancellationToken cancellationToken = default)
    {
        var existente = await _context.Set<OrcamentoCategoria>()
            .FirstOrDefaultAsync(
                o => o.UsuarioId == entity.UsuarioId
                     && o.CategoriaId == entity.CategoriaId
                     && o.PeriodoTipo == entity.PeriodoTipo
                     && o.PeriodoReferencia == entity.PeriodoReferencia,
                cancellationToken);

        if (existente is null)
        {
            await _context.Set<OrcamentoCategoria>().AddAsync(entity, cancellationToken);
            return entity;
        }

        existente.ValorLimite = entity.ValorLimite;
        existente.UpdatedAt = DateTime.UtcNow;
        _context.Set<OrcamentoCategoria>().Update(existente);
        return existente;
    }
}
