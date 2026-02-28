using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Services;

public class CategoriaService : ICategoriaService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CategoriaService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoriaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categorias = await _context.Categorias
            .Include(c => c.Produtos)
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);

        return categorias.Select(c => new CategoriaDto(
            c.Id,
            c.Nome,
            c.Descricao,
            c.CreatedAt,
            c.Produtos.Count
        ));
    }

    public async Task<CategoriaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias
            .Include(c => c.Produtos)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (categoria == null)
            return null;

        return new CategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.CreatedAt,
            categoria.Produtos.Count
        );
    }

    public async Task<CategoriaDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias
            .Include(c => c.Produtos)
            .FirstOrDefaultAsync(c => c.Nome.ToLower() == nome.ToLower(), cancellationToken);

        if (categoria == null)
            return null;

        return new CategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.CreatedAt,
            categoria.Produtos.Count
        );
    }

    public async Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            CreatedAt = MarketList.Domain.Helpers.DateTimeHelper.EnsureUtc(DateTime.UtcNow)
        };

        _context.Categorias.Add(categoria);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.CreatedAt,
            0
        );
    }

    public async Task<CategoriaDto?> UpdateAsync(Guid id, CategoriaUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias.FindAsync([id], cancellationToken);
        if (categoria == null)
            return null;

        categoria.Nome = dto.Nome;
        categoria.Descricao = dto.Descricao;
        categoria.UpdatedAt = MarketList.Domain.Helpers.DateTimeHelper.EnsureUtc(DateTime.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var count = await _context.Produtos.CountAsync(p => p.CategoriaId == id, cancellationToken);

        return new CategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.CreatedAt,
            count
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias.FindAsync([id], cancellationToken);
        if (categoria == null)
            return false;

        _context.Categorias.Remove(categoria);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
