namespace MarketList.Domain.Entities;

public class Subcategory : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<ProductCatalog> Products { get; set; } = new List<ProductCatalog>();
}
