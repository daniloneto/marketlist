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

    public ProdutoService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProdutoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _context.Produtos
            .Include(p => p.Categoria)
            .Include(p => p.HistoricoPrecos)
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken);

        return produtos.Select(p => new ProdutoDto(
            p.Id,
            p.Nome,
            p.Descricao,
            p.Unidade,
            p.CategoriaId,
            p.Categoria.Nome,
            p.HistoricoPrecos
                .OrderByDescending(h => h.DataConsulta)
                .Select(h => (decimal?)h.PrecoUnitario)
                .FirstOrDefault(),
            p.CreatedAt
        ));
    }

    public async Task<ProdutoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var produto = await _context.Produtos
            .Include(p => p.Categoria)
            .Include(p => p.HistoricoPrecos)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (produto == null)
            return null;

        return new ProdutoDto(
            produto.Id,
            produto.Nome,
            produto.Descricao,
            produto.Unidade,
            produto.CategoriaId,
            produto.Categoria.Nome,
            produto.HistoricoPrecos
                .OrderByDescending(h => h.DataConsulta)
                .Select(h => (decimal?)h.PrecoUnitario)
                .FirstOrDefault(),
            produto.CreatedAt
        );
    }

    public async Task<ProdutoDto?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var produto = await _context.Produtos
            .Include(p => p.Categoria)
            .Include(p => p.HistoricoPrecos)
            .FirstOrDefaultAsync(p => p.Nome.ToLower() == nome.ToLower(), cancellationToken);

        if (produto == null)
            return null;

        return new ProdutoDto(
            produto.Id,
            produto.Nome,
            produto.Descricao,
            produto.Unidade,
            produto.CategoriaId,
            produto.Categoria.Nome,
            produto.HistoricoPrecos
                .OrderByDescending(h => h.DataConsulta)
                .Select(h => (decimal?)h.PrecoUnitario)
                .FirstOrDefault(),
            produto.CreatedAt
        );
    }

    public async Task<IEnumerable<ProdutoDto>> GetByCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        return await _context.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId == categoriaId)
            .Select(p => new ProdutoDto(
                p.Id,
                p.Nome,
                p.Descricao,
                p.Unidade,
                p.CategoriaId,
                p.Categoria.Nome,
                p.HistoricoPrecos
                    .OrderByDescending(h => h.DataConsulta)
                    .Select(h => (decimal?)h.PrecoUnitario)
                    .FirstOrDefault(),
                p.CreatedAt
            ))
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProdutoDto> CreateAsync(ProdutoCreateDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias.FindAsync([dto.CategoriaId], cancellationToken)
            ?? throw new InvalidOperationException("Categoria não encontrada");

        var produto = new Produto
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Unidade = dto.Unidade,
            CategoriaId = dto.CategoriaId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Produtos.Add(produto);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProdutoDto(
            produto.Id,
            produto.Nome,
            produto.Descricao,
            produto.Unidade,
            produto.CategoriaId,
            categoria.Nome,
            null,
            produto.CreatedAt
        );
    }

    public async Task<ProdutoDto?> UpdateAsync(Guid id, ProdutoUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var produto = await _context.Produtos.FindAsync([id], cancellationToken);
        if (produto == null)
            return null;

        var categoria = await _context.Categorias.FindAsync([dto.CategoriaId], cancellationToken)
            ?? throw new InvalidOperationException("Categoria não encontrada");

        produto.Nome = dto.Nome;
        produto.Descricao = dto.Descricao;
        produto.Unidade = dto.Unidade;
        produto.CategoriaId = dto.CategoriaId;
        produto.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var ultimoPreco = await _context.HistoricoPrecos
            .Where(h => h.ProdutoId == id)
            .OrderByDescending(h => h.DataConsulta)
            .Select(h => (decimal?)h.PrecoUnitario)
            .FirstOrDefaultAsync(cancellationToken);

        return new ProdutoDto(
            produto.Id,
            produto.Nome,
            produto.Descricao,
            produto.Unidade,
            produto.CategoriaId,
            categoria.Nome,
            ultimoPreco,
            produto.CreatedAt
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var produto = await _context.Produtos.FindAsync([id], cancellationToken);
        if (produto == null)
            return false;

        _context.Produtos.Remove(produto);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<string> GerarListaSimplesAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _context.Produtos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Categoria.Nome)
            .ThenBy(p => p.Nome)
            .ToListAsync(cancellationToken);

        // Buscar a última quantidade de cada produto nas listas
        var produtoIds = produtos.Select(p => p.Id).ToList();
        var ultimasQuantidades = await _context.ItensListaDeCompras
            .Where(i => produtoIds.Contains(i.ProdutoId))
            .GroupBy(i => i.ProdutoId)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Quantidade = g.OrderByDescending(i => i.CreatedAt).First().Quantidade
            })
            .ToDictionaryAsync(x => x.ProdutoId, x => x.Quantidade, cancellationToken);

        // Buscar o último preço de cada produto
        var ultimosPrecos = await _context.HistoricoPrecos
            .Where(h => produtoIds.Contains(h.ProdutoId))
            .GroupBy(h => h.ProdutoId)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Preco = g.OrderByDescending(h => h.DataConsulta).First().PrecoUnitario
            })
            .ToDictionaryAsync(x => x.ProdutoId, x => x.Preco, cancellationToken);

        var agrupados = produtos.GroupBy(p => p.Categoria.Nome);

        var sb = new System.Text.StringBuilder();
        var primeiro = true;

        foreach (var grupo in agrupados)
        {
            if (!primeiro)
                sb.AppendLine();

            sb.AppendLine(grupo.Key);

            foreach (var produto in grupo)
            {
                var quantidade = ultimasQuantidades.ContainsKey(produto.Id)
                    ? ultimasQuantidades[produto.Id]
                    : 1;

                // Formatar quantidade: se for inteiro, sem casas decimais
                var qtdFormatada = quantidade % 1 == 0
                    ? quantidade.ToString("0")
                    : quantidade.ToString("0.##");

                var unidade = !string.IsNullOrWhiteSpace(produto.Unidade)
                    ? produto.Unidade.ToLower()
                    : "un";

                var precoFormatado = ultimosPrecos.ContainsKey(produto.Id)
                    ? ultimosPrecos[produto.Id].ToString("C", new System.Globalization.CultureInfo("pt-BR"))
                    : "-";

                sb.AppendLine($"{qtdFormatada} {unidade} {produto.Nome} {precoFormatado}");
            }

            primeiro = false;
        }

        return sb.ToString().TrimEnd();
    }
}
