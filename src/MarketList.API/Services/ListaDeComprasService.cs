using Hangfire;
using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Enums;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Services;

public class ListaDeComprasService : IListaDeComprasService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ListaDeComprasService(
        AppDbContext context,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
    }    public async Task<IEnumerable<ListaDeComprasDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var listas = await _context.ListasDeCompras
            .Include(l => l.Itens)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return listas.Select(l => new ListaDeComprasDto(
            l.Id,
            l.Nome,
            l.TextoOriginal,
            l.TipoEntrada,
            l.Status,
            l.CreatedAt,
            l.ProcessadoEm,
            l.ErroProcessamento,
            l.Itens.Count,
            l.Itens.Any() ? l.Itens.Sum(i => i.SubTotal ?? 0) : (decimal?)null
        ));
    }

    public async Task<ListaDeComprasDetalhadaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lista = await _context.ListasDeCompras
            .Include(l => l.Itens)
                .ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lista == null)
            return null;

        var itensDto = lista.Itens.Select(i => new ItemListaDeComprasDto(
            i.Id,
            i.ProdutoId,
            i.Produto.Nome,
            i.Produto.Unidade,
            i.Quantidade,
            i.UnidadeDeMedida,
            i.PrecoUnitario,
            i.PrecoTotal,
            i.SubTotal,
            i.TextoOriginal,
            i.Comprado
        )).ToList();

        return new ListaDeComprasDetalhadaDto(
            lista.Id,
            lista.Nome,
            lista.TextoOriginal,
            lista.TipoEntrada,
            lista.Status,
            lista.CreatedAt,
            lista.ProcessadoEm,
            lista.ErroProcessamento,
            itensDto
        );
    }    public async Task<ListaDeComprasDto> CreateAsync(ListaDeComprasCreateDto dto, CancellationToken cancellationToken = default)
    {
        var lista = new ListaDeCompras
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            TextoOriginal = dto.TextoOriginal,
            TipoEntrada = dto.TipoEntrada,
            Status = StatusLista.Pendente,
            CreatedAt = DateTime.UtcNow
        };

        _context.ListasDeCompras.Add(lista);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispara o Job do Hangfire para processamento assíncrono
        _backgroundJobClient.Enqueue<IProcessamentoListaService>(
            service => service.ProcessarListaAsync(lista.Id, CancellationToken.None));        return new ListaDeComprasDto(
            lista.Id,
            lista.Nome,
            lista.TextoOriginal,
            lista.TipoEntrada,
            lista.Status,
            lista.CreatedAt,
            lista.ProcessadoEm,
            lista.ErroProcessamento,
            0,
            null
        );
    }

    public async Task<ListaDeComprasDto?> UpdateAsync(Guid id, ListaDeComprasUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var lista = await _context.ListasDeCompras
            .Include(l => l.Itens)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lista == null)
            return null;

        lista.Nome = dto.Nome;
        lista.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);        return new ListaDeComprasDto(
            lista.Id,
            lista.Nome,
            lista.TextoOriginal,
            lista.TipoEntrada,
            lista.Status,
            lista.CreatedAt,
            lista.ProcessadoEm,
            lista.ErroProcessamento,
            lista.Itens.Count,
            lista.Itens.Any() ? lista.Itens.Sum(i => i.SubTotal ?? 0) : (decimal?)null
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lista = await _context.ListasDeCompras.FindAsync([id], cancellationToken);
        if (lista == null)
            return false;

        _context.ListasDeCompras.Remove(lista);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ItemListaDeComprasDto?> AddItemAsync(Guid listaId, ItemListaDeComprasCreateDto dto, CancellationToken cancellationToken = default)
    {
        var lista = await _context.ListasDeCompras.FindAsync([listaId], cancellationToken);
        if (lista == null)
            return null;

        var produto = await _context.Produtos.FindAsync([dto.ProdutoId], cancellationToken);
        if (produto == null)
            return null;

        // Busca último preço do produto
        var ultimoPreco = await _context.HistoricoPrecos
            .Where(h => h.ProdutoId == dto.ProdutoId)
            .OrderByDescending(h => h.DataConsulta)
            .Select(h => (decimal?)h.PrecoUnitario)
            .FirstOrDefaultAsync(cancellationToken);

        var item = new ItemListaDeCompras
        {
            Id = Guid.NewGuid(),
            ListaDeComprasId = listaId,
            ProdutoId = dto.ProdutoId,
            Quantidade = dto.Quantidade,
            PrecoUnitario = ultimoPreco,
            Comprado = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ItensListaDeCompras.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);        return new ItemListaDeComprasDto(
            item.Id,
            item.ProdutoId,
            produto.Nome,
            produto.Unidade,
            item.Quantidade,
            item.UnidadeDeMedida,
            item.PrecoUnitario,
            item.PrecoTotal,
            item.SubTotal,
            item.TextoOriginal,
            item.Comprado
        );
    }

    public async Task<ItemListaDeComprasDto?> UpdateItemAsync(Guid listaId, Guid itemId, ItemListaDeComprasUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _context.ItensListaDeCompras
            .Include(i => i.Produto)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ListaDeComprasId == listaId, cancellationToken);

        if (item == null)
            return null;

        item.Quantidade = dto.Quantidade;
        item.Comprado = dto.Comprado;
        item.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);        return new ItemListaDeComprasDto(
            item.Id,
            item.ProdutoId,
            item.Produto.Nome,
            item.Produto.Unidade,
            item.Quantidade,
            item.UnidadeDeMedida,
            item.PrecoUnitario,
            item.PrecoTotal,
            item.SubTotal,
            item.TextoOriginal,
            item.Comprado
        );
    }

    public async Task<bool> RemoveItemAsync(Guid listaId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.ItensListaDeCompras
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ListaDeComprasId == listaId, cancellationToken);

        if (item == null)
            return false;

        _context.ItensListaDeCompras.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
