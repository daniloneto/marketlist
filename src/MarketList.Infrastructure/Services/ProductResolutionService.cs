using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Enums;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Services;

public class ProductResolutionService : IProductResolutionService
{
    private readonly AppDbContext _context;
    private readonly ITextoNormalizacaoService _normalizacaoService;

    public ProductResolutionService(AppDbContext context, ITextoNormalizacaoService normalizacaoService)
    {
        _context = context;
        _normalizacaoService = normalizacaoService;
    }

    public async Task<ProductResolutionResultDto> ResolveAsync(string rawProductName, CancellationToken cancellationToken = default)
    {
        var normalized = _normalizacaoService.Normalizar(rawProductName);
        var candidates = await _context.ProductCatalog
            .Where(x => x.IsActive)
            .Include(x => x.Category)
            .ToListAsync(cancellationToken);

        ProductCatalog? best = null;
        decimal bestScore = 0;

        foreach (var candidate in candidates)
        {
            var score = Similarity(normalized, candidate.NameNormalized);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best is not null && bestScore >= 80)
        {
            return new ProductResolutionResultDto(rawProductName, best.NameCanonical, best.CategoryId, best.Category.Name, best.SubcategoryId, bestScore, ProductResolutionStatus.Auto);
        }

        return new ProductResolutionResultDto(rawProductName, null, null, null, null, bestScore, ProductResolutionStatus.PendingReview);
    }

    private static decimal Similarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;
        if (a == b) return 100;

        var distance = LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        var score = (1m - ((decimal)distance / maxLen)) * 100;

        if (a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase))
        {
            score = Math.Max(score, 85);
        }

        return Math.Round(Math.Clamp(score, 0, 100), 2);
    }

    private static int LevenshteinDistance(string source, string target)
    {
        var d = new int[source.Length + 1, target.Length + 1];
        for (var i = 0; i <= source.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= target.Length; j++) d[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                d[i, j] = new[] { d[i - 1, j] + 1, d[i, j - 1] + 1, d[i - 1, j - 1] + cost }.Min();
            }
        }

        return d[source.Length, target.Length];
    }
}
