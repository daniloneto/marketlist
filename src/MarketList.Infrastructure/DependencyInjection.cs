using MarketList.Application.Interfaces;
using MarketList.Application.Services;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Configurations;
using MarketList.Infrastructure.Data;
using MarketList.Infrastructure.Repositories;
using MarketList.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace MarketList.Infrastructure;

public static class DependencyInjection
{public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Database Options
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        var databaseOptions = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Configure DbContext with the appropriate database provider
        ConfigureDbContext(services, databaseOptions);

        // Unit of Work
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        // Register all repositories
        RegisterRepositories(services);

        // Register chatbot and MCP services
        RegisterChatbotServices(services, configuration);

        // Register integration services
        RegisterIntegrationServices(services, configuration);

        // Register application services
        RegisterApplicationServices(services);

        return services;
    }    private static void ConfigureDbContext(IServiceCollection services, DatabaseOptions databaseOptions)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var provider = databaseOptions.Provider.ToLower();
            var connectionStrings = databaseOptions.ConnectionStrings;

            switch (provider)
            {
                case "sqlite":
                    if (string.IsNullOrWhiteSpace(connectionStrings?.Sqlite))
                    {
                        throw new InvalidOperationException(
                            "SQLite connection string is not configured. " +
                            "Please set 'Database:ConnectionStrings:Sqlite' in appsettings.json");
                    }
                    options.UseSqlite(
                        connectionStrings.Sqlite,
                        sqlite => sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    );
                    break;

                case "postgres":
                    if (string.IsNullOrWhiteSpace(connectionStrings?.Postgres))
                    {
                        throw new InvalidOperationException(
                            "PostgreSQL connection string is not configured. " +
                            "Please set 'Database:ConnectionStrings:Postgres' in appsettings.json");
                    }
                    options.UseNpgsql(
                        connectionStrings.Postgres,
                        npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    );
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider: '{databaseOptions.Provider}'. " +
                        "Supported providers: 'Postgres', 'Sqlite'");
            }

            // Suppress pending model changes warning for multi-provider support
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
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
    }

    private static void RegisterChatbotServices(IServiceCollection services, IConfiguration configuration)
    {
        // Chatbot feature flag options
        var chatbotSection = configuration.GetSection("Chatbot");
        services.Configure<ChatbotOptions>(chatbotSection);
        var chatbotOptions = chatbotSection.Get<ChatbotOptions>() ?? new ChatbotOptions();

        // MCP Client Configuration - Ollama
        var mcpSection = configuration.GetSection("MCP");
        services.Configure<McpClientOptions>(mcpSection);

        if (chatbotOptions.Enabled)
        {
            // MCP HttpClient and implementation
            services.AddHttpClient<IMcpClientService, McpClientService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<McpClientOptions>>().Value;
                var integracoes = sp.GetRequiredService<IOptions<IntegracoesOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(options.Endpoint))
                {
                    var uri = new Uri(options.Endpoint);
                    client.BaseAddress = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");
                    client.Timeout = TimeSpan.FromSeconds(integracoes.TimeoutSegundos);
                }
            });

            // Chat Assistant Service and tools
            services.AddScoped<IChatAssistantService, ChatAssistantService>();
            services.AddScoped<IEmpresaResolverService, EmpresaResolverService>();
            services.AddScoped<ToolExecutor>();
        }
        else
        {
            // Register a NoOp IChatAssistantService when disabled
            services.AddScoped<IChatAssistantService, MarketList.Application.Services.DisabledChatAssistantService>();
        }
    }

    private static void RegisterIntegrationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Integracoes options
        var integracoesSection = configuration.GetSection("Integracoes");
        services.Configure<IntegracoesOptions>(integracoesSection);

        // Api options
        var apiSection = configuration.GetSection("Api");
        services.Configure<ApiOptions>(apiSection);

        // Telegram integration
        var telegramSection = configuration.GetSection("Telegram");
        services.Configure<Configurations.TelegramOptions>(telegramSection);

        services.AddHttpClient<Services.ITelegramClientService, Services.TelegramClientService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<Configurations.TelegramOptions>>().Value;
            var integracoes = sp.GetRequiredService<IOptions<IntegracoesOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSegundos > 0 ? options.TimeoutSegundos : integracoes.TimeoutSegundos);
        });
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IAnalisadorTextoService, AnalisadorTextoService>();
        services.AddScoped<ILeitorNotaFiscal, LeitorNotaFiscal>();
        services.AddScoped<IProcessamentoListaService, ProcessamentoListaService>();
        services.AddScoped<ITextoNormalizacaoService, TextoNormalizacaoService>();
        services.AddScoped<IProdutoResolverService, ProdutoResolverService>();
        services.AddScoped<ICategoriaClassificadorService, CategoriaClassificadorService>();
        services.AddScoped<IProdutoAprovacaoService, ProdutoAprovacaoService>();
    }
}
