using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;

namespace RecordVault.Functions
{
    public class CDMUtilFunction
    {
        private readonly ILogger<CDMUtilFunction> _logger;
        private readonly ICDMService _cdmService;
        private readonly ISQLSchemaManagementService _sqlSchemaManagementService;
        private readonly ICSVProcessingService _csvProcessingService;
        private readonly IAzureStorageAccountPersistence _azureStorageAccountPersistence;

        public CDMUtilFunction(
            ILogger<CDMUtilFunction> logger,
            ICDMService cdmService,
            SQLSchemaManagementFactory sqlSchemaManagementFactory,
            ICSVProcessingService csvProcessingService,
            IAzureStorageAccountPersistence azureStorageAccountPersistence
        )
        {
            _logger = logger;
            _cdmService = cdmService;
            _sqlSchemaManagementService = sqlSchemaManagementFactory.GetSQLSchemaManagementService();
            _csvProcessingService = csvProcessingService;
            _azureStorageAccountPersistence = azureStorageAccountPersistence;
        }

        [Function("CDMUtilFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // TODO This is a test function, remove this later

            var url = Environment.GetEnvironmentVariable("CDMManifestURL");
            var fileURL = "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-10T00.29.50Z/inventtable/2024.csv";

            var sqlMetadata = await _cdmService.GetCDMEntityMetadata(url, "inventtable");

            foreach (var table in sqlMetadata)
            {
                var stgTable = table.CopyAsStagingTable();

                _sqlSchemaManagementService.CreateOrUpdateTable(table);

                _sqlSchemaManagementService.CreateOrUpdateTable(stgTable);

                var storageURL = new AzureStorageURL(fileURL);

                var streamReader = await _azureStorageAccountPersistence.GetStreamReaderFromURL(storageURL);

                _csvProcessingService.CSVStreamReaderToSQL(streamReader, stgTable.TableName);
            }

            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
