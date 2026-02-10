using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.InMemory;
using MarketList.API.Jobs;
using MarketList.API.Services;
using MarketList.Application.Interfaces;
using MarketList.Application.Services;
using MarketList.Infrastructure;
using MarketList.Infrastructure.Configurations;
using MarketList.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MarketList.Domain.Entities;
using MarketList.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on the port specified by Cloud Run (or default to 8080)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Bind ApiOptions and TelegramOptions
builder.Services.Configure<MarketList.Infrastructure.Configurations.ApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

// Bind Chatbot options early so DI registration can be conditional
builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection("Chatbot"));
var chatbotOptions = builder.Configuration.GetSection("Chatbot").Get<ChatbotOptions>() ?? new ChatbotOptions();

// Add services to the container
builder.Services.AddControllers();
// Authorization: require authenticated by default, allow exceptions with [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MarketList API", Version = "v1" });
});

// CORS - read allowed origins from configuration
var apiOptions = builder.Configuration.GetSection("Api").Get<MarketList.Infrastructure.Configurations.ApiOptions>() ?? new MarketList.Infrastructure.Configurations.ApiOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        if (apiOptions.AllowedOrigins != null && apiOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(apiOptions.AllowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // No hardcoded origins in code — fall back to permissive policy when not configured
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Infrastructure (DbContext, Repositories, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Configure JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key");
var jwtIssuer = jwtSection.GetValue<string>("Issuer");
var jwtAudience = jwtSection.GetValue<string>("Audience");
var jwtExpMinutes = jwtSection.GetValue<int>("ExpirationMinutes", 60);

if (!string.IsNullOrWhiteSpace(jwtKey))
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    builder.Services.AddAuthentication(options =>
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

// Application Services
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IHistoricoPrecoService, HistoricoPrecoService>();
builder.Services.AddScoped<IListaDeComprasService, ListaDeComprasService>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();

// Application Layer Services (para processamento)
builder.Services.AddScoped<IAnalisadorTextoService, AnalisadorTextoService>();
builder.Services.AddScoped<IProcessamentoListaService, ProcessamentoListaService>();

// Jobs do Hangfire
builder.Services.AddScoped<ProcessamentoListaJob>();
builder.Services.AddScoped<LimpezaHistoricoJob>();

// Configure Hangfire based on database provider
var databaseOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
ConfigureHangfire(builder.Services, databaseOptions);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (sem autenticação para desenvolvimento)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Em produção, adicionar autenticação
    Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
});

// Map controllers conditionally: only add ChatController routes if Chatbot enabled
if (chatbotOptions.Enabled)
{
    app.MapControllers();
}
else
{
    // Map all controllers except ChatController - use custom convention: register controllers normally but avoid mapping route for ChatController
    // Simpler approach: don't map controllers and map endpoints for other controllers manually is complex. Instead, keep MapControllers but prevent ChatController from being discoverable by removing its route metadata at startup.
    // As a safe and simple alternative, we will keep MapControllers but add a middleware to return 404 for any request under /api/chat when chatbot disabled.
    app.MapControllers();
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

// Aplicar migrations automaticamente
try
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Iniciando aplicação de migrations...");
        
        var context = scope.ServiceProvider.GetRequiredService<MarketList.Infrastructure.Data.AppDbContext>();
        await context.Database.MigrateAsync();
        
        // Seed admin user if missing
        try
        {
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            var exists = await context.Set<Usuario>().AnyAsync(u => u.Login == "admin");
            if (!exists)
            {
                var admin = new Usuario
                {
                    Id = Guid.NewGuid(),
                    Login = "admin",
                    SenhaHash = passwordService.HashSenha("admin")
                };
                context.Set<Usuario>().Add(admin);
                await context.SaveChangesAsync();
                logger.LogInformation("Usuário admin criado (login=admin)." );
            }
        }
        catch (Exception ex)
        {
            var logger2 = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger2.LogError(ex, "Erro ao criar usuário admin");
        }
        
        logger.LogInformation("Migrations aplicadas com sucesso!");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Erro ao aplicar migrations");
    throw new InvalidOperationException("Erro ao aplicar migrations durante inicialização da aplicação.", ex);
}

// Configurar Jobs Recorrentes do Hangfire
ConfigurarJobsRecorrentes();

await app.RunAsync();

void ConfigureHangfire(IServiceCollection services, DatabaseOptions databaseOptions)
{
    services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseRecommendedSerializerSettings();

        // Configure storage based on database provider
        var provider = databaseOptions.Provider.ToLower();
        
        if (provider == "sqlite")
        {
            // Use InMemory storage for Hangfire when using SQLite (no persistent job storage needed for now)
            config.UseInMemoryStorage();
        }
        else if (provider == "postgres")
        {
            // PostgreSQL storage for Hangfire
            var connectionString = databaseOptions.ConnectionStrings?.Postgres 
                ?? throw new InvalidOperationException("PostgreSQL connection string not configured");
            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            });
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported database provider for Hangfire: '{databaseOptions.Provider}'");
        }
    });

    services.AddHangfireServer();
}

void ConfigurarJobsRecorrentes()
{
    // Limpeza de histórico - executa diariamente às 3h da manhã
    RecurringJob.AddOrUpdate<LimpezaHistoricoJob>(
        "limpeza-historico",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 3 * * *", // Cron: todos os dias às 3:00
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
}

