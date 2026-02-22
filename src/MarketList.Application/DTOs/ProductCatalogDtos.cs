using MarketList.Domain.Enums;

namespace MarketList.Application.DTOs;

public record ProductCatalogDto(
    Guid Id,
    string NameCanonical,
    string NameNormalized,
    Guid CategoryId,
    string CategoryName,
    Guid? SubcategoryId,
    string? SubcategoryName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ProductCatalogCreateDto(
    string NameCanonical,
    Guid CategoryId,
    Guid? SubcategoryId
);

public record ProductCatalogUpdateDto(
    string NameCanonical,
    Guid CategoryId,
    Guid? SubcategoryId,
    bool IsActive
);

public record CatalogCategoryDto(Guid Id, string Name);
public record CatalogCategoryCreateDto(string Name);

public record CatalogSubcategoryDto(Guid Id, Guid CategoryId, string CategoryName, string Name);
public record CatalogSubcategoryCreateDto(Guid CategoryId, string Name);

public record ProductResolutionResultDto(
    string RawName,
    string? ResolvedName,
    Guid? CategoryId,
    Guid? SubcategoryId,
    decimal Score,
    ProductResolutionStatus Status);
