namespace MarketList.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
    public virtual ICollection<ProductCatalog> Products { get; set; } = new List<ProductCatalog>();
}
