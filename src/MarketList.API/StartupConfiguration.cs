using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using MarketList.API.Jobs;
using MarketList.API.Services;
using MarketList.Application.Interfaces;
using MarketList.Application.Services;
using MarketList.Domain.Entities;
using MarketList.Infrastructure.Configurations;
using MarketList.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MarketList.API;

internal static class StartupConfiguration
{
    public static void ConfigureWebHost(WebApplicationBuilder builder)
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    public static ChatbotOptions ConfigureOptions(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
        builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
        builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection("Chatbot"));

        return builder.Configuration.GetSection("Chatbot").Get<ChatbotOptions>() ?? new ChatbotOptions();
    }

    public static ApiOptions ConfigureApiOptions(WebApplicationBuilder builder)
    {
        return builder.Configuration.GetSection("Api").Get<ApiOptions>() ?? new ApiOptions();
    }

    public static void ConfigureMvcAndAuth(IServiceCollection services)
    {
        services.AddControllers();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        services.AddEndpointsApiExplorer();
    }

    public static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "FinControl API", Version = "v1" });
        });
    }

    public static void ConfigureCors(IServiceCollection services, ApiOptions apiOptions)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowReactApp", policy =>
            {
                if (apiOptions.AllowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(apiOptions.AllowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                    return;
                }

                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    public static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection.GetValue<string>("Key");
        var jwtIssuer = jwtSection.GetValue<string>("Issuer");
        var jwtAudience = jwtSection.GetValue<string>("Audience");

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            return;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };
        });
    }

    public static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProdutoService, ProdutoService>();
        services.AddScoped<IHistoricoPrecoService, HistoricoPrecoService>();
        services.AddScoped<IListaDeComprasService, ListaDeComprasService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IOrcamentoCategoriaService, OrcamentoCategoriaService>();

        services.AddScoped<IAnalisadorTextoService, AnalisadorTextoService>();
        services.AddScoped<IProcessamentoListaService, ProcessamentoListaService>();
    }

    public static void RegisterJobs(IServiceCollection services)
    {
        services.AddScoped<ProcessamentoListaJob>();
        services.AddScoped<LimpezaHistoricoJob>();
    }

    public static void ConfigurePipeline(WebApplication app, ChatbotOptions chatbotOptions)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseCors("AllowReactApp");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
        });

        app.MapControllers();

        if (!chatbotOptions.Enabled)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/chat", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(new { error = "Chatbot desativado" });
                    return;
                }

                await next();
            });
        }
    }

    public static async Task ApplyMigrationsAndSeedAdminAsync(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Iniciando aplicacao de migrations...");

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();
            await EnsureAdminUserAsync(scope.ServiceProvider, context);
            var csvCatalogImportService = scope.ServiceProvider.GetRequiredService<ICsvCatalogImportService>();
            await csvCatalogImportService.ImportAsync();

            logger.LogInformation("Migrations aplicadas com sucesso.");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao aplicar migrations");
            throw new InvalidOperationException("Erro ao aplicar migrations durante inicializacao da aplicacao.", ex);
        }
    }

    private static async Task EnsureAdminUserAsync(IServiceProvider serviceProvider, AppDbContext context)
    {
        try
        {
            var passwordService = serviceProvider.GetRequiredService<IPasswordService>();
            var exists = await context.Set<Usuario>().AnyAsync(u => u.Login == "admin");
            if (exists)
            {
                return;
            }

            var admin = new Usuario
            {
                Id = Guid.NewGuid(),
                Login = "admin",
                SenhaHash = passwordService.HashSenha("admin")
            };

            context.Set<Usuario>().Add(admin);
            await context.SaveChangesAsync();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Usuario admin criado (login=admin).");
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao criar usuario admin");
        }
    }

    public static void ConfigureHangfire(IServiceCollection services, DatabaseOptions databaseOptions)
    {
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            ConfigureHangfireStorage(config, databaseOptions);
        });

        services.AddHangfireServer();
    }

    private static void ConfigureHangfireStorage(IGlobalConfiguration config, DatabaseOptions databaseOptions)
    {
        var provider = databaseOptions.Provider.ToLowerInvariant();

        if (provider == "sqlite")
        {
            config.UseInMemoryStorage();
            return;
        }

        if (provider == "postgres")
        {
            var connectionString = databaseOptions.ConnectionStrings?.Postgres
                ?? throw new InvalidOperationException("PostgreSQL connection string not configured");

            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            });
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported database provider for Hangfire: '{databaseOptions.Provider}'");
    }

    public static void ConfigurarJobsRecorrentes()
    {
        RecurringJob.AddOrUpdate<LimpezaHistoricoJob>(
            "limpeza-historico",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 3 * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
    }
}
