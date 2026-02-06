using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class HistoricoPrecoConfiguration : IEntityTypeConfiguration<HistoricoPreco>
{
    public void Configure(EntityTypeBuilder<HistoricoPreco> builder)
    {
        builder.ToTable("historico_precos");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id");

        builder.Property(h => h.ProdutoId)
            .HasColumnName("produto_id");

        builder.Property(h => h.PrecoUnitario)
            .HasColumnName("preco_unitario")
            .HasPrecision(18, 2);

        builder.Property(h => h.DataConsulta)
            .HasColumnName("data_consulta");

        builder.Property(h => h.FontePreco)
            .HasColumnName("fonte_preco")
            .HasMaxLength(100);

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(h => h.EmpresaId)
            .HasColumnName("empresa_id");

        builder.HasOne(h => h.Produto)
            .WithMany(p => p.HistoricoPrecos)
            .HasForeignKey(h => h.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Empresa)
            .WithMany(e => e.HistoricoPrecos)
            .HasForeignKey(h => h.EmpresaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => new { h.ProdutoId, h.DataConsulta });
        builder.HasIndex(h => h.EmpresaId);
    }
}
