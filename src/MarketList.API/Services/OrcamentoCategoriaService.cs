using System.Globalization;
using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Enums;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Services;

public class OrcamentoCategoriaService : IOrcamentoCategoriaService
{
    private const decimal LimiteAlertaPercentual = 80m;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrcamentoCategoriaRepository _orcamentoRepository;

    public OrcamentoCategoriaService(
        AppDbContext context,
        IUnitOfWork unitOfWork,
        IOrcamentoCategoriaRepository orcamentoRepository)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _orcamentoRepository = orcamentoRepository;
    }

    public async Task<OrcamentoCategoriaDto> CriarOuAtualizarAsync(
        Guid usuarioId,
        CriarOrcamentoCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId, cancellationToken);

        if (categoria is null)
        {
            throw new InvalidOperationException($"Categoria {request.CategoriaId} nao encontrada.");
        }

        if (request.ValorLimite < 0)
        {
            throw new InvalidOperationException("Valor limite nao pode ser negativo.");
        }

        var periodoReferencia = ResolverPeriodoReferencia(request.PeriodoTipo, request.PeriodoReferencia, DateTime.UtcNow);

        var entity = new OrcamentoCategoria
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            CategoriaId = request.CategoriaId,
            PeriodoTipo = request.PeriodoTipo,
            PeriodoReferencia = periodoReferencia,
            ValorLimite = request.ValorLimite
        };

        var salvo = await _orcamentoRepository.UpsertAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrcamentoCategoriaDto(
            salvo.Id,
            salvo.UsuarioId,
            salvo.CategoriaId,
            categoria.Nome,
            salvo.PeriodoTipo,
            salvo.PeriodoReferencia,
            salvo.ValorLimite,
            salvo.CreatedAt,
            salvo.UpdatedAt);
    }

    public async Task<IEnumerable<OrcamentoCategoriaDto>> ListarPorPeriodoAsync(
        Guid usuarioId,
        PeriodoOrcamentoTipo periodoTipo,
        string? periodoReferencia,
        CancellationToken cancellationToken = default)
    {
        var referencia = ResolverPeriodoReferencia(periodoTipo, periodoReferencia, DateTime.UtcNow);
        var orcamentos = await _orcamentoRepository.ListByPeriodoAsync(usuarioId, periodoTipo, referencia, cancellationToken);

        return orcamentos.Select(o => new OrcamentoCategoriaDto(
            o.Id,
            o.UsuarioId,
            o.CategoriaId,
            o.Categoria.Nome,
            o.PeriodoTipo,
            o.PeriodoReferencia,
            o.ValorLimite,
            o.CreatedAt,
            o.UpdatedAt));
    }

    public async Task<ResumoOrcamentoListaDto?> ObterResumoParaListaAsync(
        Guid usuarioId,
        Guid listaId,
        PeriodoOrcamentoTipo periodoTipo = PeriodoOrcamentoTipo.Mensal,
        CancellationToken cancellationToken = default)
    {
        var lista = await _context.ListasDeCompras
            .AsNoTracking()
            .Include(l => l.Itens)
                .ThenInclude(i => i.Produto)
                    .ThenInclude(p => p.Categoria)
            .FirstOrDefaultAsync(l => l.Id == listaId, cancellationToken);

        if (lista is null)
        {
            return null;
        }

        var dataReferencia = MarketList.Domain.Helpers.DateTimeHelper.EnsureUtc(lista.DataCompra) ?? MarketList.Domain.Helpers.DateTimeHelper.EnsureUtc(DateTime.UtcNow);
        var periodoReferencia = ResolverPeriodoReferencia(periodoTipo, null, dataReferencia);

        var produtoIds = lista.Itens
            .Select(i => i.ProdutoId)
            .Distinct()
            .ToList();

        var ultimosPrecos = await ObterUltimosPrecosPorProdutoAsync(produtoIds, cancellationToken);
        var totaisPorCategoria = new Dictionary<Guid, (string Nome, decimal Total, int ItensSemPreco)>();

        foreach (var item in lista.Itens)
        {
            var categoriaId = item.Produto.CategoriaId;
            var nomeCategoria = item.Produto.Categoria.Nome;
            var precoUnitario = ultimosPrecos.GetValueOrDefault(item.ProdutoId);
            var itemSemPreco = precoUnitario <= 0m;
            var totalItem = item.Quantidade * precoUnitario;

            if (!totaisPorCategoria.TryGetValue(categoriaId, out var acumulado))
            {
                acumulado = (nomeCategoria, 0m, 0);
            }

            totaisPorCategoria[categoriaId] = (
                acumulado.Nome,
                acumulado.Total + totalItem,
                acumulado.ItensSemPreco + (itemSemPreco ? 1 : 0));
        }

        var orcamentos = await _orcamentoRepository
            .ListByPeriodoAsync(usuarioId, periodoTipo, periodoReferencia, cancellationToken);

        var limitePorCategoria = orcamentos
            .ToDictionary(o => o.CategoriaId, o => o.ValorLimite);

        var itensResumo = totaisPorCategoria
            .Select(kvp =>
            {
                var limite = limitePorCategoria.GetValueOrDefault(kvp.Key, 0m);
                var total = kvp.Value.Total;
                var percentual = CalcularPercentualConsumido(total, limite);
                var status = ResolverStatus(total, limite, percentual);

                return new ItemResumoOrcamentoCategoriaDto(
                    kvp.Key,
                    kvp.Value.Nome,
                    total,
                    kvp.Value.ItensSemPreco,
                    limite,
                    percentual,
                    status);
            })
            .OrderBy(i => i.NomeCategoria)
            .ToList();

        return new ResumoOrcamentoListaDto(
            lista.Id,
            periodoReferencia,
            periodoTipo,
            itensResumo.Sum(i => i.TotalEstimado),
            itensResumo.Sum(i => i.ItensSemPreco),
            itensResumo);
    }

    private async Task<Dictionary<Guid, decimal>> ObterUltimosPrecosPorProdutoAsync(
        IReadOnlyCollection<Guid> produtoIds,
        CancellationToken cancellationToken)
    {
        if (produtoIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var historicos = await _context.HistoricoPrecos
            .AsNoTracking()
            .Where(h => produtoIds.Contains(h.ProdutoId))
            .OrderByDescending(h => h.DataConsulta)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return historicos
            .GroupBy(h => h.ProdutoId)
            .ToDictionary(g => g.Key, g => g.First().PrecoUnitario);
    }

    private static decimal CalcularPercentualConsumido(decimal total, decimal limite)
    {
        if (limite <= 0m)
        {
            return total > 0m ? 100m : 0m;
        }

        var percentual = (total / limite) * 100m;
        return percentual < 0m ? 0m : percentual;
    }

    private static StatusConsumoOrcamento ResolverStatus(decimal total, decimal limite, decimal percentual)
    {
        if (limite <= 0m)
        {
            return total > 0m ? StatusConsumoOrcamento.Estourado : StatusConsumoOrcamento.Normal;
        }

        if (percentual >= 100m)
        {
            return StatusConsumoOrcamento.Estourado;
        }

        if (percentual >= LimiteAlertaPercentual)
        {
            return StatusConsumoOrcamento.Alerta;
        }

        return StatusConsumoOrcamento.Normal;
    }

    public static string ResolverPeriodoReferencia(
        PeriodoOrcamentoTipo periodoTipo,
        string? periodoReferencia,
        DateTime dataReferencia)
    {
        if (!string.IsNullOrWhiteSpace(periodoReferencia))
        {
            return periodoReferencia.Trim();
        }

        return periodoTipo switch
        {
            PeriodoOrcamentoTipo.Semanal => $"{dataReferencia.Year}-W{ISOWeek.GetWeekOfYear(dataReferencia):00}",
            PeriodoOrcamentoTipo.Mensal => $"{dataReferencia:yyyy-MM}",
            _ => throw new InvalidOperationException("Periodo de orcamento invalido.")
        };
    }
}
