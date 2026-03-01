using MarketList.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Helpers;

public static class PaginacaoHelper
{
    public static readonly int[] TamanhosPermitidos = [10, 20, 50, 100];

    public static bool TryValidar(int pageNumber, int pageSize, out string? erro)
    {
        if (pageNumber < 1)
        {
            erro = "pageNumber deve ser no mínimo 1.";
            return false;
        }

        if (!TamanhosPermitidos.Contains(pageSize))
        {
            erro = $"pageSize inválido. Valores permitidos: {string.Join(", ", TamanhosPermitidos)}.";
            return false;
        }

        erro = null;
        return true;
    }

    public static async Task<PagedResultDto<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<T>(items, totalCount, pageNumber, pageSize, totalPages);
    }
}
