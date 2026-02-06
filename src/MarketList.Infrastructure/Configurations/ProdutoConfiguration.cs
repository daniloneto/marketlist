using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.NomeNormalizado)
            .HasColumnName("nome_normalizado")
            .HasMaxLength(200);

        builder.Property(p => p.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500);        builder.Property(p => p.Unidade)
            .HasColumnName("unidade")
            .HasMaxLength(20);

        builder.Property(p => p.CodigoLoja)
            .HasColumnName("codigo_loja")
            .HasMaxLength(50);

        builder.Property(p => p.PrecisaRevisao)
            .HasColumnName("precisa_revisao")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.CategoriaPrecisaRevisao)
            .HasColumnName("categoria_precisa_revisao")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Produtos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Sinonimos)
            .WithOne(s => s.Produto)
            .HasForeignKey(s => s.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Nome);
        builder.HasIndex(p => p.NomeNormalizado);
        builder.HasIndex(p => p.CodigoLoja);
    }
}
