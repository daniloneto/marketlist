using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Enums;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Services;

public class ProductResolutionService : IProductResolutionService
{
    private const double ContainmentLengthRatioThreshold = 0.6;

    private readonly AppDbContext _context;
    private readonly ITextoNormalizacaoService _normalizacaoService;

    public ProductResolutionService(AppDbContext context, ITextoNormalizacaoService normalizacaoService)
    {
        _context = context;
        _normalizacaoService = normalizacaoService;
    }

    public async Task<IReadOnlyList<ProductCatalogResolutionCandidateDto>> GetActiveCatalogSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductCatalog
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Category)
            .Select(x => new ProductCatalogResolutionCandidateDto(
                x.NameCanonical,
                x.NameNormalized,
                x.CategoryId,
                x.Category.Name,
                x.SubcategoryId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductResolutionResultDto> ResolveAsync(string rawProductName, IReadOnlyList<ProductCatalogResolutionCandidateDto>? catalogSnapshot = null, CancellationToken cancellationToken = default)
    {
        var normalized = _normalizacaoService.Normalizar(rawProductName);
        var candidates = catalogSnapshot ?? await GetActiveCatalogSnapshotAsync(cancellationToken);

        ProductCatalogResolutionCandidateDto? best = null;
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
            return new ProductResolutionResultDto(rawProductName, best.NameCanonical, best.CategoryId, best.CategoryName, best.SubcategoryId, bestScore, ProductResolutionStatus.Auto);
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
            var ratio = (double)Math.Min(a.Length, b.Length) / Math.Max(a.Length, b.Length);
            if (ratio >= ContainmentLengthRatioThreshold)
            {
                score = Math.Max(score, 85);
            }
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
