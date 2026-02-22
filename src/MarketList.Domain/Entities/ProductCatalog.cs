namespace MarketList.Domain.Entities;

public class ProductCatalog : BaseEntity
{
    public string NameCanonical { get; set; } = string.Empty;
    public string NameNormalized { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Category Category { get; set; } = null!;
    public virtual Subcategory? Subcategory { get; set; }
}
