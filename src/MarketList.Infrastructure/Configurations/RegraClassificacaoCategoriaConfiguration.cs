using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class RegraClassificacaoCategoriaConfiguration : IEntityTypeConfiguration<RegraClassificacaoCategoria>
{
    public void Configure(EntityTypeBuilder<RegraClassificacaoCategoria> builder)
    {
        builder.ToTable("regras_classificacao_categoria");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.TermoNormalizado)
            .HasColumnName("termo_normalizado")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Prioridade)
            .HasColumnName("prioridade")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(r => r.ContagemUsos)
            .HasColumnName("contagem_usos")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(r => r.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(r => r.Categoria)
            .WithMany()
            .HasForeignKey(r => r.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice único para prevenir regras duplicadas
        builder.HasIndex(r => r.TermoNormalizado)
            .IsUnique();
    }
}
