namespace MarketList.Application.DTOs;

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
