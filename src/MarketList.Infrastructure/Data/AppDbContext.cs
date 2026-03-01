using MarketList.Domain.Entities;
using MarketList.Domain.Helpers;
using MarketList.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketList.Infrastructure.Data;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<HistoricoPreco> HistoricoPrecos => Set<HistoricoPreco>();
    public DbSet<ListaDeCompras> ListasDeCompras => Set<ListaDeCompras>();
    public DbSet<ItemListaDeCompras> ItensListaDeCompras => Set<ItemListaDeCompras>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<SinonimoProduto> SinonimosProduto => Set<SinonimoProduto>();
    public DbSet<RegraClassificacaoCategoria> RegrasClassificacaoCategoria => Set<RegraClassificacaoCategoria>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<OrcamentoCategoria> OrcamentosCategoria => Set<OrcamentoCategoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Atualiza timestamps automaticamente
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // Enforce UTC for all DateTime/DateTime? properties in all entities
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            var entity = entry.Entity;
            if (entity == null) continue;

            var properties = entity.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(DateTime))
                {
                    var value = (DateTime)prop.GetValue(entity)!;
                    if (value != default)
                    {
                        prop.SetValue(entity, DateTimeHelper.EnsureUtc(value));
                    }
                }
                else if (prop.PropertyType == typeof(DateTime?))
                {
                    var value = (DateTime?)prop.GetValue(entity);
                    if (value.HasValue)
                    {
                        prop.SetValue(entity, (DateTime?)DateTimeHelper.EnsureUtc(value.Value));
                    }
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
