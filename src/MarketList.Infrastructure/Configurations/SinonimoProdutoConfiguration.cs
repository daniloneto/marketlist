using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class SinonimoProdutoConfiguration : IEntityTypeConfiguration<SinonimoProduto>
{
    public void Configure(EntityTypeBuilder<SinonimoProduto> builder)
    {
        builder.ToTable("sinonimos_produto");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.TextoOriginal)
            .HasColumnName("texto_original")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(s => s.TextoNormalizado)
            .HasColumnName("texto_normalizado")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(s => s.FonteOrigem)
            .HasColumnName("fonte_origem")
            .HasMaxLength(50);

        builder.Property(s => s.ProdutoId)
            .HasColumnName("produto_id");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(s => s.Produto)
            .WithMany(p => p.Sinonimos)
            .HasForeignKey(s => s.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice para busca rápida por texto normalizado
        builder.HasIndex(s => s.TextoNormalizado);
    }
}
