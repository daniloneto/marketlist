using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class ProductCatalogConfiguration : IEntityTypeConfiguration<ProductCatalog>
{
    public void Configure(EntityTypeBuilder<ProductCatalog> builder)
    {
        builder.ToTable("product_catalog");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.NameCanonical).HasColumnName("name_canonical").HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameNormalized).HasColumnName("name_normalized").HasMaxLength(200).IsRequired();
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.SubcategoryId).HasColumnName("subcategory_id");
        builder.Property(x => x.LegacyProdutoId).HasColumnName("legacy_produto_id");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subcategory)
            .WithMany(s => s.Products)
            .HasForeignKey(x => x.SubcategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LegacyProduto)
            .WithMany()
            .HasForeignKey(x => x.LegacyProdutoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.NameNormalized).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.LegacyProdutoId).IsUnique();
    }
}
