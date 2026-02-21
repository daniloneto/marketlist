using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class OrcamentoCategoriaConfiguration : IEntityTypeConfiguration<OrcamentoCategoria>
{
    public void Configure(EntityTypeBuilder<OrcamentoCategoria> builder)
    {
        builder.ToTable("orcamentos_categoria");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(o => o.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(o => o.PeriodoTipo)
            .HasColumnName("periodo_tipo")
            .HasConversion<int>();

        builder.Property(o => o.PeriodoReferencia)
            .HasColumnName("periodo_referencia")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.ValorLimite)
            .HasColumnName("valor_limite")
            .HasPrecision(18, 2);

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(o => o.Categoria)
            .WithMany(c => c.Orcamentos)
            .HasForeignKey(o => o.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Usuario)
            .WithMany()
            .HasForeignKey(o => o.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.UsuarioId, o.PeriodoTipo, o.PeriodoReferencia, o.CategoriaId })
            .IsUnique();
        builder.HasIndex(o => new { o.UsuarioId, o.PeriodoTipo, o.PeriodoReferencia });
        builder.HasIndex(o => o.CategoriaId);
    }
}
