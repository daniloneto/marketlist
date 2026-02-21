using MarketList.API;
using MarketList.Infrastructure;
using MarketList.Infrastructure.Configurations;

var builder = WebApplication.CreateBuilder(args);

StartupConfiguration.ConfigureWebHost(builder);
var chatbotOptions = StartupConfiguration.ConfigureOptions(builder);
var apiOptions = StartupConfiguration.ConfigureApiOptions(builder);

StartupConfiguration.ConfigureMvcAndAuth(builder.Services);
StartupConfiguration.ConfigureSwagger(builder.Services);
StartupConfiguration.ConfigureCors(builder.Services, apiOptions);

builder.Services.AddInfrastructure(builder.Configuration);
StartupConfiguration.ConfigureJwtAuthentication(builder.Services, builder.Configuration);
StartupConfiguration.RegisterApplicationServices(builder.Services);
StartupConfiguration.RegisterJobs(builder.Services);

var databaseOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
StartupConfiguration.ConfigureHangfire(builder.Services, databaseOptions);

var app = builder.Build();

StartupConfiguration.ConfigurePipeline(app, chatbotOptions);
await StartupConfiguration.ApplyMigrationsAndSeedAdminAsync(app);
StartupConfiguration.ConfigurarJobsRecorrentes();

await app.RunAsync();
