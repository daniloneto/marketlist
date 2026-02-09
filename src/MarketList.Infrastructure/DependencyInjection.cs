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

        // Chatbot feature flag options
        var chatbotSection = configuration.GetSection("Chatbot");
        services.Configure<ChatbotOptions>(chatbotSection);
        var chatbotOptions = chatbotSection.Get<ChatbotOptions>() ?? new ChatbotOptions();

        // MCP Client Configuration - Ollama
        var mcpSection = configuration.GetSection("MCP");
        services.Configure<McpClientOptions>(mcpSection);

        // Integracoes options
        var integracoesSection = configuration.GetSection("Integracoes");
        services.Configure<IntegracoesOptions>(integracoesSection);

        // Api options
        var apiSection = configuration.GetSection("Api");
        services.Configure<ApiOptions>(apiSection);

        // Register external integration HttpClients that are always needed (ex: Telegram)
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

        // Conditionally register MCP, Chat Assistant and related services only when feature is enabled
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
            // When disabled, avoid registering any MCP/LLM HttpClients or services that may trigger external calls.
            // Register a NoOp IChatAssistantService so controllers can still be resolved but will return controlled errors.
            services.AddScoped<IChatAssistantService, MarketList.Application.Services.DisabledChatAssistantService>();
        }

        // Application Services (always registered)
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
