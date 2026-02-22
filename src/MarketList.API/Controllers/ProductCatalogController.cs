using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/catalog-products")]
public class ProductCatalogController : ControllerBase
{
    private readonly IProductCatalogService _service;

    public ProductCatalogController(IProductCatalogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductCatalogDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProductCatalogDto>> Create(ProductCatalogCreateDto dto, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductCatalogDto>> Update(Guid id, ProductCatalogUpdateDto dto, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, dto, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => await _service.DeactivateAsync(id, cancellationToken) ? NoContent() : NotFound();
}
