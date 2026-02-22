using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Services;

public class CatalogTaxonomyService : ICatalogTaxonomyService
{
    private readonly AppDbContext _context;

    public CatalogTaxonomyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CatalogCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await _context.CatalogCategories.AsNoTracking().OrderBy(x => x.Name).Select(x => new CatalogCategoryDto(x.Id, x.Name)).ToListAsync(cancellationToken);

    public async Task<CatalogCategoryDto> CreateCategoryAsync(CatalogCategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Category { Id = Guid.NewGuid(), Name = dto.Name.Trim(), CreatedAt = DateTime.UtcNow };
        _context.CatalogCategories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return new CatalogCategoryDto(entity.Id, entity.Name);
    }

    public async Task<CatalogCategoryDto?> UpdateCategoryAsync(Guid id, CatalogCategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CatalogCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return null;

        entity.Name = dto.Name.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return new CatalogCategoryDto(entity.Id, entity.Name);
    }

    public async Task<DeleteCategoryResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CatalogCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return DeleteCategoryResult.NotFound;

        var hasProducts = await _context.ProductCatalog.AnyAsync(x => x.CategoryId == id && x.IsActive, cancellationToken);
        var hasSubcategories = await _context.CatalogSubcategories.AnyAsync(x => x.CategoryId == id, cancellationToken);

        if (hasProducts || hasSubcategories)
            return DeleteCategoryResult.HasDependencies;

        _context.CatalogCategories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return DeleteCategoryResult.Deleted;
    }

    public async Task<IReadOnlyList<CatalogSubcategoryDto>> GetSubcategoriesAsync(CancellationToken cancellationToken = default)
        => await _context.CatalogSubcategories.AsNoTracking().Include(x => x.Category).OrderBy(x => x.Name)
            .Select(x => new CatalogSubcategoryDto(x.Id, x.CategoryId, x.Category.Name, x.Name)).ToListAsync(cancellationToken);

    public async Task<CatalogSubcategoryDto> CreateSubcategoryAsync(CatalogSubcategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _context.CatalogCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.CategoryId, cancellationToken);

        if (category is null)
            throw new InvalidOperationException($"Categoria {dto.CategoryId} não encontrada");

        var entity = new Subcategory { Id = Guid.NewGuid(), CategoryId = dto.CategoryId, Name = dto.Name.Trim(), CreatedAt = DateTime.UtcNow };
        _context.CatalogSubcategories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new CatalogSubcategoryDto(entity.Id, entity.CategoryId, category.Name, entity.Name);
    }
}
