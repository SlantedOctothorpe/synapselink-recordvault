using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RecordVault.Application.Factories;
using RecordVault.Application.Services;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Infrastructure.FileProcessing.Common;
using RecordVault.Infrastructure.FileProcessing.CSV;
using RecordVault.Infrastructure.FileProcessing.Parquet;
using RecordVault.Infrastructure.Persistence;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging();

        // Persistence
        services.AddScoped<SQLPersistenceFactory>();
        services.AddScoped<SQLServerPersistence>()
            .AddScoped<ISQLPersistence, SQLServerPersistence>(sp => sp.GetRequiredService<SQLServerPersistence>());

        // Schema Management
        services.AddScoped<SQLSchemaManagementFactory>();
        services.AddScoped<SQLServerSchemaService>()
            .AddScoped<ISQLSchemaManagementService, SQLServerSchemaService>(sp => sp.GetRequiredService<SQLServerSchemaService>());

        // Azure Storage
        services.AddTransient<IAzureStorageAccountPersistence, AzureStorageAccountPersistence>();

        // Application Services
        services.AddTransient<ICDMService, CDMService>();
        services.AddTransient<IDataSyncService, DataSyncService>();
        services.AddTransient<IEntitySyncService, EntitySyncService>();

        // File Processing
        services.AddScoped<IFileProcessingService, FileProcessor>();
        services.AddScoped<CSVFileReader>();
        services.AddScoped<ParquetFileReader>();
        services.AddScoped<FileReaderFactory>();
        services.AddScoped<IProducer, Producer>();
        services.AddScoped<IConsumer, Consumer>();
    })
    .Build();

host.Run();