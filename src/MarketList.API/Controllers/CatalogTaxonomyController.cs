using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/admin/catalog-taxonomy")]
public class CatalogTaxonomyController : ControllerBase
{
    private readonly ICatalogTaxonomyService _service;

    public CatalogTaxonomyController(ICatalogTaxonomyService service)
    {
        _service = service;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CatalogCategoryDto>>> GetCategories(CancellationToken cancellationToken)
        => Ok(await _service.GetCategoriesAsync(cancellationToken));

    [HttpPost("categories")]
    public async Task<ActionResult<CatalogCategoryDto>> CreateCategory(CatalogCategoryCreateDto dto, CancellationToken cancellationToken)
        => Ok(await _service.CreateCategoryAsync(dto, cancellationToken));

    [HttpGet("subcategories")]
    public async Task<ActionResult<IReadOnlyList<CatalogSubcategoryDto>>> GetSubcategories(CancellationToken cancellationToken)
        => Ok(await _service.GetSubcategoriesAsync(cancellationToken));

    [HttpPost("subcategories")]
    public async Task<ActionResult<CatalogSubcategoryDto>> CreateSubcategory(CatalogSubcategoryCreateDto dto, CancellationToken cancellationToken)
        => Ok(await _service.CreateSubcategoryAsync(dto, cancellationToken));
}
