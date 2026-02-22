using System.Globalization;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketList.Infrastructure.Services;

public class CatalogSeedService : ICatalogSeedService
{
    private const string Header = "Categoria,Subcategoria,Produto";
    private readonly AppDbContext _context;
    private readonly ITextoNormalizacaoService _normalizacaoService;
    private readonly ILogger<CatalogSeedService> _logger;

    public CatalogSeedService(AppDbContext context, ITextoNormalizacaoService normalizacaoService, ILogger<CatalogSeedService> logger)
    {
        _context = context;
        _normalizacaoService = normalizacaoService;
        _logger = logger;
    }

    public async Task SeedFromCsvAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "supermercado.csv");
        if (!File.Exists(filePath))
        {
            filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "MarketList.Infrastructure", "supermercado.csv"));
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("CSV de seed não encontrado: {Path}", filePath);
            return;
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0 || !string.Equals(lines[0].Trim(), Header, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cabeçalho inválido no CSV. Esperado: {Header}");
        }

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var parts = lines[i].Split(',');
            if (parts.Length < 3) continue;

            var categoryName = parts[0].Trim();
            var subcategoryName = parts[1].Trim();
            var productName = parts[2].Trim();

            if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(productName)) continue;

            var category = await _context.CatalogCategories.FirstOrDefaultAsync(x => x.Name == categoryName, cancellationToken);
            if (category is null)
            {
                category = new Category { Id = Guid.NewGuid(), Name = categoryName };
                _context.CatalogCategories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);
            }

            Subcategory? subcategory = null;
            if (!string.IsNullOrWhiteSpace(subcategoryName))
            {
                subcategory = await _context.CatalogSubcategories
                    .FirstOrDefaultAsync(x => x.CategoryId == category.Id && x.Name == subcategoryName, cancellationToken);
                if (subcategory is null)
                {
                    subcategory = new Subcategory { Id = Guid.NewGuid(), CategoryId = category.Id, Name = subcategoryName };
                    _context.CatalogSubcategories.Add(subcategory);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var normalizedName = _normalizacaoService.Normalizar(productName);
            var exists = await _context.ProductCatalog.AnyAsync(x => x.NameNormalized == normalizedName, cancellationToken);
            if (!exists)
            {
                _context.ProductCatalog.Add(new ProductCatalog
                {
                    Id = Guid.NewGuid(),
                    NameCanonical = productName,
                    NameNormalized = normalizedName,
                    CategoryId = category.Id,
                    SubcategoryId = subcategory?.Id,
                    IsActive = true
                });
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
