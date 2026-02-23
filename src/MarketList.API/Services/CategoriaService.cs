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
        var categorias = await _context.CatalogCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoriaDto(
                c.Id,
                c.Name,
                null,
                c.CreatedAt,
                c.Products.Count(p => p.IsActive)
            ))
            .ToListAsync(cancellationToken);

        return categorias;
    }

    public async Task<CategoriaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.CatalogCategories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaDto(
                c.Id,
                c.Name,
                null,
                c.CreatedAt,
                c.Products.Count(p => p.IsActive)
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (categoria == null)
            return null;

        return categoria;
    }

    public async Task<CategoriaDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.CatalogCategories
            .AsNoTracking()
            .Where(c => c.Name.ToLower() == nome.ToLower())
            .Select(c => new CategoriaDto(
                c.Id,
                c.Name,
                null,
                c.CreatedAt,
                c.Products.Count(p => p.IsActive)
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (categoria == null)
            return null;

        return categoria;
    }

    public async Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Nome,
            CreatedAt = DateTime.UtcNow
        };

        _context.CatalogCategories.Add(categoria);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoriaDto(
            categoria.Id,
            categoria.Name,
            null,
            categoria.CreatedAt,
            0
        );
    }

    public async Task<CategoriaDto?> UpdateAsync(Guid id, CategoriaUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.CatalogCategories.FindAsync([id], cancellationToken);
        if (categoria == null)
            return null;

        categoria.Name = dto.Nome;
        categoria.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var count = await _context.ProductCatalog.CountAsync(p => p.CategoryId == id && p.IsActive, cancellationToken);

        return new CategoriaDto(
            categoria.Id,
            categoria.Name,
            null,
            categoria.CreatedAt,
            count
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.CatalogCategories.FindAsync([id], cancellationToken);
        if (categoria == null)
            return false;

        var hasDependencies = await _context.ProductCatalog.AnyAsync(x => x.CategoryId == id && x.IsActive, cancellationToken)
            || await _context.CatalogSubcategories.AnyAsync(x => x.CategoryId == id, cancellationToken);

        if (hasDependencies)
            return false;

        _context.CatalogCategories.Remove(categoria);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
