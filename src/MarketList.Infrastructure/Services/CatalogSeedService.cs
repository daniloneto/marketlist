using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketList.Infrastructure.Services;

public class CatalogSeedService : ICatalogSeedService
{
    private readonly AppDbContext _context;
    private readonly ITextoNormalizacaoService _normalizacaoService;
    private readonly ILogger<CatalogSeedService> _logger;
    private readonly string? _configuredCsvPath;

    public CatalogSeedService(AppDbContext context, ITextoNormalizacaoService normalizacaoService, ILogger<CatalogSeedService> logger, IConfiguration configuration)
    {
        _context = context;
        _normalizacaoService = normalizacaoService;
        _logger = logger;
        _configuredCsvPath = configuration["CatalogSeed:CsvPath"];
    }

    public async Task SeedFromCsvAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.ProductCatalog.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seed de catálogo ignorado: tabela product_catalog já possui dados.");
            await SyncLegacyCatalogAsync(cancellationToken);
            return;
        }

        var filePath = ResolveCsvPath();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, csvConfig);

        await csv.ReadAsync();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? [];
        if (headers.Length != 3 || headers[0] != "Categoria" || headers[1] != "Subcategoria" || headers[2] != "Produto")
        {
            throw new InvalidOperationException("Cabeçalho inválido no CSV. Esperado: Categoria,Subcategoria,Produto");
        }

        var categoriesByName = await _context.CatalogCategories.ToDictionaryAsync(x => x.Name, x => x, cancellationToken);
        var subcategoriesByKey = await _context.CatalogSubcategories
            .ToDictionaryAsync(x => $"{x.CategoryId}:{x.Name}", x => x, cancellationToken);
        var productsByNormalized = await _context.ProductCatalog
            .ToDictionaryAsync(x => x.NameNormalized, x => x, cancellationToken);

        while (await csv.ReadAsync())
        {
            var row = new CatalogCsvRow
            {
                Categoria = csv.GetField("Categoria")?.Trim(),
                Subcategoria = csv.GetField("Subcategoria")?.Trim(),
                Produto = csv.GetField("Produto")?.Trim()
            };

            if (string.IsNullOrWhiteSpace(row.Categoria) || string.IsNullOrWhiteSpace(row.Produto))
            {
                continue;
            }

            if (!categoriesByName.TryGetValue(row.Categoria, out var category))
            {
                category = new Category { Id = Guid.NewGuid(), Name = row.Categoria };
                _context.CatalogCategories.Add(category);
                categoriesByName[row.Categoria] = category;
            }

            Subcategory? subcategory = null;
            if (!string.IsNullOrWhiteSpace(row.Subcategoria))
            {
                var subKey = $"{category.Id}:{row.Subcategoria}";
                if (!subcategoriesByKey.TryGetValue(subKey, out subcategory))
                {
                    subcategory = new Subcategory { Id = Guid.NewGuid(), CategoryId = category.Id, Name = row.Subcategoria };
                    _context.CatalogSubcategories.Add(subcategory);
                    subcategoriesByKey[subKey] = subcategory;
                }
            }

            var normalizedName = _normalizacaoService.Normalizar(row.Produto);
            if (productsByNormalized.ContainsKey(normalizedName))
            {
                continue;
            }

            var productCatalog = new ProductCatalog
            {
                Id = Guid.NewGuid(),
                NameCanonical = row.Produto,
                NameNormalized = normalizedName,
                CategoryId = category.Id,
                SubcategoryId = subcategory?.Id,
                IsActive = true
            };

            _context.ProductCatalog.Add(productCatalog);
            productsByNormalized[normalizedName] = productCatalog;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await SyncLegacyCatalogAsync(cancellationToken);
    }

    private string ResolveCsvPath()
    {
        if (!string.IsNullOrWhiteSpace(_configuredCsvPath))
        {
            var configuredPath = Path.GetFullPath(_configuredCsvPath);
            if (File.Exists(configuredPath))
            {
                return configuredPath;
            }

            throw new InvalidOperationException($"CSV de seed não encontrado no caminho configurado 'CatalogSeed:CsvPath': {configuredPath}");
        }

        var assemblyDirectory = Path.GetDirectoryName(typeof(CatalogSeedService).Assembly.Location);
        var baseDirectory = AppContext.BaseDirectory;

        var searchRoots = new[] { assemblyDirectory, baseDirectory }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct();

        foreach (var startDirectory in searchRoots)
        {
            var current = new DirectoryInfo(startDirectory);

            while (current is not null)
            {
                var directMatch = current.GetFiles("supermercado.csv", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (directMatch is not null)
                {
                    return directMatch.FullName;
                }

                var srcInfrastructure = Path.Combine(current.FullName, "src", "MarketList.Infrastructure");
                if (Directory.Exists(srcInfrastructure))
                {
                    var scopedMatch = Directory.EnumerateFiles(srcInfrastructure, "supermercado.csv", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(scopedMatch))
                    {
                        return scopedMatch;
                    }
                }

                current = current.Parent;
            }
        }

        throw new InvalidOperationException("CSV de seed não encontrado. Configure 'CatalogSeed:CsvPath' ou disponibilize 'supermercado.csv' no diretório da aplicação/projeto.");
    }

    private async Task SyncLegacyCatalogAsync(CancellationToken cancellationToken)
    {
        var legacyCategoriesByName = await _context.Categorias
            .ToDictionaryAsync(x => x.Nome, x => x, cancellationToken);

        var catalogCategories = await _context.CatalogCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        foreach (var catalogCategory in catalogCategories)
        {
            if (legacyCategoriesByName.ContainsKey(catalogCategory.Name))
            {
                continue;
            }

            var legacyCategory = new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = catalogCategory.Name,
                Descricao = "Importada do catálogo de referência"
            };

            _context.Categorias.Add(legacyCategory);
            legacyCategoriesByName[legacyCategory.Nome] = legacyCategory;
        }

        var legacyProductsByNormalized = await _context.Produtos
            .Where(x => x.NomeNormalizado != null)
            .ToDictionaryAsync(x => x.NomeNormalizado!, x => x, cancellationToken);

        var catalogProducts = await _context.ProductCatalog
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var catalogProduct in catalogProducts)
        {
            if (!legacyCategoriesByName.TryGetValue(catalogProduct.Category.Name, out var legacyCategory))
            {
                continue;
            }

            if (legacyProductsByNormalized.ContainsKey(catalogProduct.NameNormalized))
            {
                continue;
            }

            var legacyProduct = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = catalogProduct.NameCanonical,
                NomeNormalizado = catalogProduct.NameNormalized,
                CategoriaId = legacyCategory.Id,
                Unidade = "un"
            };

            _context.Produtos.Add(legacyProduct);
            legacyProductsByNormalized[legacyProduct.NomeNormalizado!] = legacyProduct;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private sealed class CatalogCsvRow
    {
        public string? Categoria { get; set; }
        public string? Subcategoria { get; set; }
        public string? Produto { get; set; }
    }
}
