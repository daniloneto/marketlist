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
        var entity = new Category { Id = Guid.NewGuid(), Name = dto.Name.Trim() };
        _context.CatalogCategories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return new CatalogCategoryDto(entity.Id, entity.Name);
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

        var entity = new Subcategory { Id = Guid.NewGuid(), CategoryId = dto.CategoryId, Name = dto.Name.Trim() };
        _context.CatalogSubcategories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new CatalogSubcategoryDto(entity.Id, entity.CategoryId, category.Name, entity.Name);
    }
}
