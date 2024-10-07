using Microsoft.Extensions.Logging;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Application.Services
{
    public class DataSyncService : IDataSyncService
    {
        private readonly ILogger<DataSyncService> _logger;
        private readonly IAzureStorageAccountPersistence _azureStorageAccountPersistence;
        private readonly ICDMService _cdmService;
        private readonly ICSVProcessingService _csvProcessingService;

        public DataSyncService(
            ILogger<DataSyncService> logger,
            IAzureStorageAccountPersistence azureStorageAccountPersistence,
            ICDMService cdmService,
            ICSVProcessingService csvProcessingService
        )
        {
            _logger = logger;
            _azureStorageAccountPersistence = azureStorageAccountPersistence;
            _cdmService = cdmService;
            _csvProcessingService = csvProcessingService;
        }

        public async Task SyncStorageAccountFile(string fileURL)
        {
            var storageURL = new AzureStorageURL(fileURL);

            var cdmEntity = _cdmService.GetCDMEntityNameFromStorageAccountURL(storageURL);

            var stagingPrefix = Environment.GetEnvironmentVariable("RecordVaultDBStagingTablePrefix");

            var stagingTableName =  $"{stagingPrefix}{cdmEntity}";

            // Get CDMService to return SQL statements

            // Execute SQL statements

            var streamReader = await _azureStorageAccountPersistence.GetStreamReaderFromURL(storageURL);

            var sqlConnectionString = Environment.GetEnvironmentVariable("RecodVaultDBConnectionString") ?? "";
            var sqlType = Environment.GetEnvironmentVariable("RecodVaultDBType") ?? "";

            _csvProcessingService.CSVStreamReaderToSQL(streamReader, stagingTableName,
                connectionString: sqlConnectionString, sqlType: sqlType);
           
            // loop here?

            // run merge proc

            // calc stats

            // log stats

            // Process enum values to SQL table

            // push event to downstream apps
        }
    }
}
