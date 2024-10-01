using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RecordVault.Application.Factories;
using RecordVault.Application.Services;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Infrastructure.Persistence;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging();

        services.AddScoped<SQLPersistenceFactory>();
        services.AddScoped<SQLServerPersistence>()
            .AddScoped<ISQLPersistence, SQLServerPersistence>(sp => sp.GetRequiredService<SQLServerPersistence>());

        services.AddTransient<IAzureStorageAccountPersistence, AzureStorageAccountPersistence>();
        services.AddTransient<ICSVProcessingService, SylvanCSVService>();
        services.AddTransient<ICDMService, CDMService>();

        services.AddTransient<IDataSyncService, DataSyncService>();
    })
    .Build();

host.Run();
