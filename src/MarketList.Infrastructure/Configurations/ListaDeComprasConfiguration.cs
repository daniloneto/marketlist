using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class ListaDeComprasConfiguration : IEntityTypeConfiguration<ListaDeCompras>
{
    public void Configure(EntityTypeBuilder<ListaDeCompras> builder)
    {
        builder.ToTable("listas_de_compras");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();        builder.Property(l => l.TextoOriginal)
            .HasColumnName("texto_original");

        builder.Property(l => l.TipoEntrada)
            .HasColumnName("tipo_entrada")
            .HasConversion<int>();

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasConversion<int>();

        builder.Property(l => l.ProcessadoEm)
            .HasColumnName("processado_em");

        builder.Property(l => l.ErroProcessamento)
            .HasColumnName("erro_processamento")
            .HasMaxLength(2000);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.CreatedAt);
    }
}
