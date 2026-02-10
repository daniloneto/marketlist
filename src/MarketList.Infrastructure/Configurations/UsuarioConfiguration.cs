using MarketList.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketList.Infrastructure.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
         builder.ToTable("usuarios");
         builder.HasKey(u => u.Id);

         builder.Property(u => u.Id)
             .ValueGeneratedOnAdd()
             .HasColumnName("id");

         builder.Property(u => u.CreatedAt)
             .IsRequired()
             .HasColumnName("created_at");

         builder.Property(u => u.UpdatedAt)
             .HasColumnName("updated_at");

        builder.Property(u => u.Login)
               .IsRequired()
               .HasMaxLength(100)
               .HasColumnName("login");

        builder.Property(u => u.SenhaHash)
               .IsRequired()
               .HasColumnName("senha_hash");

        builder.HasIndex(u => u.Login).IsUnique();
    }
}
