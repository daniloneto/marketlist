using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class ItemListaDeComprasConfiguration : IEntityTypeConfiguration<ItemListaDeCompras>
{
    public void Configure(EntityTypeBuilder<ItemListaDeCompras> builder)
    {
        builder.ToTable("itens_lista_de_compras");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.ListaDeComprasId)
            .HasColumnName("lista_de_compras_id");

        builder.Property(i => i.ProdutoId)
            .HasColumnName("produto_id");

        builder.Property(i => i.Quantidade)
            .HasColumnName("quantidade")
            .HasPrecision(18, 3);

        builder.Property(i => i.PrecoUnitario)
            .HasColumnName("preco_unitario")
            .HasPrecision(18, 2);

        builder.Property(i => i.TextoOriginal)
            .HasColumnName("texto_original")
            .HasMaxLength(500);

        builder.Property(i => i.Comprado)
            .HasColumnName("comprado");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Ignore(i => i.SubTotal);

        builder.HasOne(i => i.ListaDeCompras)
            .WithMany(l => l.Itens)
            .HasForeignKey(i => i.ListaDeComprasId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Produto)
            .WithMany(p => p.ItensLista)
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.ListaDeComprasId);
    }
}
