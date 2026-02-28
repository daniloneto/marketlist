using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketList.Infrastructure.Services;

public class CsvCatalogImportService : ICsvCatalogImportService
{
    private readonly AppDbContext _context;
    private readonly ITextoNormalizacaoService _textoNormalizacaoService;
    private readonly ILogger<CsvCatalogImportService> _logger;

    public CsvCatalogImportService(
        AppDbContext context,
        ITextoNormalizacaoService textoNormalizacaoService,
        ILogger<CsvCatalogImportService> logger)
    {
        _context = context;
        _textoNormalizacaoService = textoNormalizacaoService;
        _logger = logger;
    }

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "supermercado.csv");
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("Arquivo CSV não encontrado em {Path}. Importação inicial ignorada.", csvPath);
            return;
        }

        var existingCategories = await _context.Categorias
            .ToDictionaryAsync(c => c.Nome.Trim(), StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingProducts = await _context.Produtos
            .Where(p => !string.IsNullOrWhiteSpace(p.NomeNormalizado))
            .Select(p => p.NomeNormalizado!)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        var newCategories = 0;
        var newProducts = 0;

        var lines = await File.ReadAllLinesAsync(csvPath, cancellationToken);
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = ParseCsvLine(line);
            if (columns.Count < 3)
            {
                continue;
            }

            var categoriaNome = columns[0].Trim();
            var produtoNome = columns[2].Trim();

            if (string.IsNullOrWhiteSpace(categoriaNome) || string.IsNullOrWhiteSpace(produtoNome))
            {
                continue;
            }

            if (!existingCategories.TryGetValue(categoriaNome, out var categoria))
            {
                categoria = new Categoria
                {
                    Id = Guid.NewGuid(),
                    Nome = categoriaNome,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Categorias.Add(categoria);
                existingCategories[categoriaNome] = categoria;
                newCategories++;
            }

            var nomeNormalizado = _textoNormalizacaoService.Normalizar(produtoNome);
            if (existingProducts.Contains(nomeNormalizado))
            {
                continue;
            }

            var produto = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = produtoNome,
                NomeNormalizado = nomeNormalizado,
                CategoriaId = categoria.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Produtos.Add(produto);
            existingProducts.Add(nomeNormalizado);
            newProducts++;
        }

        if (newCategories == 0 && newProducts == 0)
        {
            _logger.LogInformation("Importação CSV concluída sem novas inserções.");
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Importação CSV concluída. Categorias inseridas: {Categories}, produtos inseridos: {Products}.",
            newCategories,
            newProducts);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }
}
