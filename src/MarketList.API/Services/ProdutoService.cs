using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Services;

public class ProdutoService : IProdutoService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITextoNormalizacaoService _normalizacaoService;

    public ProdutoService(AppDbContext context, IUnitOfWork unitOfWork, ITextoNormalizacaoService normalizacaoService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _normalizacaoService = normalizacaoService;
    }

    public async Task<IEnumerable<ProdutoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _context.ProductCatalog
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.NameCanonical)
            .Select(p => new ProdutoDto(
                p.Id,
                p.NameCanonical,
                null,
                null,
                p.CategoryId,
                p.Category.Name,
                null,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return produtos;
    }

    public async Task<ProdutoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var produto = await _context.ProductCatalog
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new ProdutoDto(
                p.Id,
                p.NameCanonical,
                null,
                null,
                p.CategoryId,
                p.Category.Name,
                null,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (produto == null)
            return null;

        return produto;
    }

    public async Task<ProdutoDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var normalized = _normalizacaoService.Normalizar(nome);
        var produto = await _context.ProductCatalog
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.NameNormalized == normalized && p.IsActive)
            .Select(p => new ProdutoDto(
                p.Id,
                p.NameCanonical,
                null,
                null,
                p.CategoryId,
                p.Category.Name,
                null,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (produto == null)
            return null;

        return produto;
    }

    public async Task<IEnumerable<ProdutoDto>> GetByCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductCatalog
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoriaId && p.IsActive)
            .Select(p => new ProdutoDto(
                p.Id,
                p.NameCanonical,
                null,
                null,
                p.CategoryId,
                p.Category.Name,
                null,
                p.CreatedAt
            ))
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProdutoDto> CreateAsync(ProdutoCreateDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.CatalogCategories.FindAsync([dto.CategoriaId], cancellationToken)
            ?? throw new InvalidOperationException("Categoria não encontrada");

        var produto = new ProductCatalog
        {
            Id = Guid.NewGuid(),
            NameCanonical = dto.Nome,
            NameNormalized = _normalizacaoService.Normalizar(dto.Nome),
            CategoryId = dto.CategoriaId,
            SubcategoryId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductCatalog.Add(produto);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProdutoDto(
            produto.Id,
            produto.NameCanonical,
            null,
            null,
            produto.CategoryId,
            categoria.Name,
            null,
            produto.CreatedAt
        );
    }

    public async Task<ProdutoDto?> UpdateAsync(Guid id, ProdutoUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var produto = await _context.ProductCatalog.FindAsync([id], cancellationToken);
        if (produto == null)
            return null;

        var categoria = await _context.CatalogCategories.FindAsync([dto.CategoriaId], cancellationToken)
            ?? throw new InvalidOperationException("Categoria não encontrada");

        produto.NameCanonical = dto.Nome;
        produto.NameNormalized = _normalizacaoService.Normalizar(dto.Nome);
        produto.CategoryId = dto.CategoriaId;
        produto.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProdutoDto(
            produto.Id,
            produto.NameCanonical,
            null,
            null,
            produto.CategoryId,
            categoria.Name,
            null,
            produto.CreatedAt
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var produto = await _context.ProductCatalog.FindAsync([id], cancellationToken);
        if (produto == null)
            return false;

        produto.IsActive = false;
        produto.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<string> GerarListaSimplesAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _context.ProductCatalog
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category.Name)
            .ThenBy(p => p.NameCanonical)
            .ToListAsync(cancellationToken);
        var agrupados = produtos.GroupBy(p => p.Category.Name);

        var sb = new System.Text.StringBuilder();
        var primeiro = true;

        foreach (var grupo in agrupados)
        {
            if (!primeiro)
                sb.AppendLine();

            sb.AppendLine(grupo.Key);

            foreach (var produto in grupo)
            {
                sb.AppendLine($"1 un {produto.NameCanonical} -");
            }

            primeiro = false;
        }

        return sb.ToString().TrimEnd();
    }
}
