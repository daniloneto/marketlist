using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Services;

public class ProductCatalogService : IProductCatalogService
{
    private readonly AppDbContext _context;
    private readonly ITextoNormalizacaoService _normalizacao;

    public ProductCatalogService(AppDbContext context, ITextoNormalizacaoService normalizacao)
    {
        _context = context;
        _normalizacao = normalizacao;
    }

    public async Task<IReadOnlyList<ProductCatalogDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Query().ToListAsync(cancellationToken);

    public async Task<ProductCatalogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ProductCatalogDto> CreateAsync(ProductCatalogCreateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new ProductCatalog
        {
            Id = Guid.NewGuid(),
            NameCanonical = dto.NameCanonical.Trim(),
            NameNormalized = _normalizacao.Normalizar(dto.NameCanonical),
            CategoryId = dto.CategoryId,
            SubcategoryId = dto.SubcategoryId,
            IsActive = true
        };

        _context.ProductCatalog.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ProductCatalogDto?> UpdateAsync(Guid id, ProductCatalogUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductCatalog.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        entity.NameCanonical = dto.NameCanonical.Trim();
        entity.NameNormalized = _normalizacao.Normalizar(dto.NameCanonical);
        entity.CategoryId = dto.CategoryId;
        entity.SubcategoryId = dto.SubcategoryId;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductCatalog.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<ProductCatalogDto> Query()
        => _context.ProductCatalog
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Subcategory)
            .OrderBy(x => x.NameCanonical)
            .Select(x => new ProductCatalogDto(
                x.Id,
                x.NameCanonical,
                x.NameNormalized,
                x.CategoryId,
                x.Category.Name,
                x.SubcategoryId,
                x.Subcategory != null ? x.Subcategory.Name : null,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt));
}
