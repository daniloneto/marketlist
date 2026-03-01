using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

public interface IListaDeComprasService
{
    Task<PagedResultDto<ListaDeComprasDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ListaDeComprasDetalhadaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ListaDeComprasDto> CreateAsync(ListaDeComprasCreateDto dto, CancellationToken cancellationToken = default);
    Task<ListaDeComprasDto?> UpdateAsync(Guid id, ListaDeComprasUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Item operations
    Task<ItemListaDeComprasDto?> AddItemAsync(Guid listaId, ItemListaDeComprasCreateDto dto, CancellationToken cancellationToken = default);
    Task<ItemListaDeComprasDto?> UpdateItemAsync(Guid listaId, Guid itemId, ItemListaDeComprasUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveItemAsync(Guid listaId, Guid itemId, CancellationToken cancellationToken = default);
}
