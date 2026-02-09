using MarketList.Application.Interfaces;
using MarketList.Application.Services;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using MarketList.Infrastructure.Repositories;
using MarketList.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketList.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Entity Framework
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
        );

        // Unit of Work
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        // Repositories - Generic
        services.AddScoped<IRepository<Categoria>, Repository<Categoria>>();
        services.AddScoped<IRepository<HistoricoPreco>, Repository<HistoricoPreco>>();
        services.AddScoped<IRepository<ListaDeCompras>, Repository<ListaDeCompras>>();
        services.AddScoped<IRepository<ItemListaDeCompras>, Repository<ItemListaDeCompras>>();
        
        // Repositories - Specialized
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<ISinonimoRepository, SinonimoRepository>();
        services.AddScoped<IRegraClassificacaoRepository, RegraClassificacaoRepository>();
        services.AddScoped<IListaDeComprasRepository, ListaDeComprasRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IHistoricoPrecoRepository, HistoricoPrecoRepository>();

        // MCP Client Configuration - Ollama
        var mcpSection = configuration.GetSection("MCP");
        services.Configure<McpClientOptions>(mcpSection);
        
        services.AddHttpClient<IMcpClientService, McpClientService>()
            .ConfigureHttpClient((provider, client) =>
            {
                var options = mcpSection.Get<McpClientOptions>();
                if (options?.Endpoint != null)
                {
                    var uri = new Uri(options.Endpoint);
                    client.BaseAddress = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");
                    client.Timeout = TimeSpan.FromMinutes(2); // Ollama pode demorar
                }
            });

        // Telegram options
        var telegramSection = configuration.GetSection("Telegram");
        services.Configure<Configurations.TelegramOptions>(telegramSection);

        services.AddHttpClient<Services.ITelegramClientService, Services.TelegramClientService>();

        // Chat Assistant Service
        services.AddScoped<IChatAssistantService, ChatAssistantService>();
        services.AddScoped<IEmpresaResolverService, EmpresaResolverService>();
        services.AddScoped<ToolExecutor>();        // Application Services
        services.AddScoped<IAnalisadorTextoService, AnalisadorTextoService>();
        services.AddScoped<ILeitorNotaFiscal, LeitorNotaFiscal>();
        services.AddScoped<IProcessamentoListaService, ProcessamentoListaService>();
        services.AddScoped<ITextoNormalizacaoService, TextoNormalizacaoService>();
        services.AddScoped<IProdutoResolverService, ProdutoResolverService>();
        services.AddScoped<ICategoriaClassificadorService, CategoriaClassificadorService>();
        services.AddScoped<IProdutoAprovacaoService, ProdutoAprovacaoService>();

        return services;
    }
}
