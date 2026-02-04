using Hangfire;
using Hangfire.PostgreSql;
using MarketList.API.Jobs;
using MarketList.API.Services;
using MarketList.Application.Interfaces;
using MarketList.Application.Services;
using MarketList.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MarketList API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Infrastructure (DbContext, Repositories, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Application Services
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IHistoricoPrecoService, HistoricoPrecoService>();
builder.Services.AddScoped<IListaDeComprasService, ListaDeComprasService>();

// Application Layer Services (para processamento)
builder.Services.AddScoped<IAnalisadorTextoService, AnalisadorTextoService>();
builder.Services.AddScoped<IProcessamentoListaService, ProcessamentoListaService>();

// Jobs do Hangfire
builder.Services.AddScoped<ProcessamentoListaJob>();
builder.Services.AddScoped<AtualizacaoPrecosJob>();
builder.Services.AddScoped<LimpezaHistoricoJob>();

// Hangfire
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(options =>
          {
              options.UseNpgsqlConnection(connectionString);
          });
});

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

app.UseAuthorization();

// Hangfire Dashboard (sem autenticação para desenvolvimento)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Em produção, adicionar autenticação
    Authorization = []
});

app.MapControllers();

// Criar banco de dados se não existir (apenas desenvolvimento)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MarketList.Infrastructure.Data.AppDbContext>();
    context.Database.EnsureCreated();
}

// Configurar Jobs Recorrentes do Hangfire
ConfigurarJobsRecorrentes();

app.Run();

void ConfigurarJobsRecorrentes()
{
    // Atualização de preços - executa a cada 6 horas
    RecurringJob.AddOrUpdate<AtualizacaoPrecosJob>(
        "atualizacao-precos",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 */6 * * *", // Cron: a cada 6 horas
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

    // Limpeza de histórico - executa diariamente às 3h da manhã
    RecurringJob.AddOrUpdate<LimpezaHistoricoJob>(
        "limpeza-historico",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 3 * * *", // Cron: todos os dias às 3:00
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
}
