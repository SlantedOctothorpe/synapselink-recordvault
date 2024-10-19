using Microsoft.Extensions.Logging;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;

namespace RecordVault.Application.Services
{
    public class DataSyncService : IDataSyncService
    {
        private readonly ILogger<DataSyncService> _logger;
        private readonly IAzureStorageAccountPersistence _azureStorageAccountPersistence;
        private readonly ICDMService _cdmService;
        private readonly IFileProcessingService _fileProcessingService;

        public DataSyncService(
            ILogger<DataSyncService> logger,
            IAzureStorageAccountPersistence azureStorageAccountPersistence,
            ICDMService cdmService,
            IFileProcessingService fileProcessingService
        )
        {
            _logger = logger;
            _azureStorageAccountPersistence = azureStorageAccountPersistence;
            _cdmService = cdmService;
            _fileProcessingService = fileProcessingService;
        }

        public async Task SyncStorageAccountFile(string fileURL)
        {
            var storageURL = new AzureStorageURL(fileURL);

            var cdmEntity = _cdmService.GetCDMEntityNameFromStorageAccountURL(storageURL);

            var stagingPrefix = Environment.GetEnvironmentVariable("RecordVaultDBStagingTablePrefix");

            var stagingTableName =  $"{stagingPrefix}{cdmEntity}";

            // Get CDMService to return SQL statements

            // Execute SQL statements

            using var stream = await _azureStorageAccountPersistence.GetStreamFromURL(storageURL);

            var sqlConnectionString = Environment.GetEnvironmentVariable("RecodVaultDBConnectionString") ?? "";
            var sqlType = Environment.GetEnvironmentVariable("RecodVaultDBType") ?? "";

            var fileType = DetermineFileType(fileURL);

            await _fileProcessingService.ProcessFileToSQL(stream, fileType, stagingTableName, sqlConnectionString, sqlType);


            // loop here?

            // run merge proc

            // calc stats

            // log stats

            // Process enum values to SQL table

            // push event to downstream apps
        }

        private string DetermineFileType(string fileURL)
        {
            string extension = Path.GetExtension(fileURL).ToLower();
            return extension switch
            {
                ".csv" => "csv",
                ".parquet" => "parquet",
                _ => throw new NotSupportedException($"Unsupported file type: {extension}")
            };
        }
    }
}
