using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Services;

public class HistoricoPrecoService : IHistoricoPrecoService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public HistoricoPrecoService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<HistoricoPrecoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var historicos = await _context.HistoricoPrecos
            .Include(h => h.Produto)
            .OrderByDescending(h => h.DataConsulta)
            .ToListAsync(cancellationToken);

        return historicos.Select(h => new HistoricoPrecoDto(
            h.Id,
            h.ProdutoId,
            h.Produto.Nome,
            h.PrecoUnitario,
            h.DataConsulta,
            h.FontePreco
        ));
    }

    public async Task<IEnumerable<HistoricoPrecoDto>> GetByProdutoAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        var historicos = await _context.HistoricoPrecos
            .Include(h => h.Produto)
            .Where(h => h.ProdutoId == produtoId)
            .OrderByDescending(h => h.DataConsulta)
            .ToListAsync(cancellationToken);

        return historicos.Select(h => new HistoricoPrecoDto(
            h.Id,
            h.ProdutoId,
            h.Produto.Nome,
            h.PrecoUnitario,
            h.DataConsulta,
            h.FontePreco
        ));
    }

    public async Task<HistoricoPrecoDto?> GetUltimoPrecoAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        var historico = await _context.HistoricoPrecos
            .Include(h => h.Produto)
            .Where(h => h.ProdutoId == produtoId)
            .OrderByDescending(h => h.DataConsulta)
            .FirstOrDefaultAsync(cancellationToken);

        if (historico == null)
            return null;

        return new HistoricoPrecoDto(
            historico.Id,
            historico.ProdutoId,
            historico.Produto.Nome,
            historico.PrecoUnitario,
            historico.DataConsulta,
            historico.FontePreco
        );
    }

    public async Task<HistoricoPrecoDto> CreateAsync(HistoricoPrecoCreateDto dto, CancellationToken cancellationToken = default)
    {
        var produto = await _context.Produtos.FindAsync([dto.ProdutoId], cancellationToken)
            ?? throw new InvalidOperationException("Produto não encontrado");

        var historico = new HistoricoPreco
        {
            Id = Guid.NewGuid(),
            ProdutoId = dto.ProdutoId,
            PrecoUnitario = dto.PrecoUnitario,
            DataConsulta = DateTime.UtcNow,
            FontePreco = dto.FontePreco,
            CreatedAt = DateTime.UtcNow
        };

        _context.HistoricoPrecos.Add(historico);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new HistoricoPrecoDto(
            historico.Id,
            historico.ProdutoId,
            produto.Nome,
            historico.PrecoUnitario,
            historico.DataConsulta,
            historico.FontePreco
        );
    }
}
