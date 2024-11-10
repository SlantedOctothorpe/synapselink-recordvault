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
        private readonly IDataSyncService _dataSyncService; 
        private readonly ISQLPersistence _sqlPersistence;

        public CDMUtilFunction(
            ILogger<CDMUtilFunction> logger,
            ICDMService cdmService,
            SQLSchemaManagementFactory sqlSchemaManagementFactory,
            IAzureStorageAccountPersistence azureStorageAccountPersistence,
            IDataSyncService dataSyncService,
            SQLPersistenceFactory sqlPersistenceFactory
        )
        {
            _logger = logger;
            _cdmService = cdmService;
            _sqlSchemaManagementService = sqlSchemaManagementFactory.GetSQLSchemaManagementService();
            _dataSyncService = dataSyncService;
            _sqlPersistence = sqlPersistenceFactory.GetSQLPersistence();
        }

        [Function("CDMUtilFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // TODO This is a test function, remove this later

            var url = Environment.GetEnvironmentVariable("CDMManifestURL") ?? "";
            var fileURLs = new Dictionary<string, List<string>>
            {
                { "pricediscgroup",
                    new List<string> {
                        "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-28T11.43.37Z/pricediscgroup/1900.csv",
                        //"https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-29T23.43.36Z/pricediscgroup/1900.csv",
                        //"https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-29T23.43.36Z/pricediscgroup/1.csv"
                     }
                },
                //{ "inventtable",
                //    new List<string> {
                //        "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-30T12.43.37Z/inventtable/2023.csv"
                //    }
                //}
            };
            //var fileURL = "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-28T11.43.37Z/pricediscgroup/1900.csv";
            //var fileURL = "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-29T23.43.36Z/pricediscgroup/1900.csv";
            //var fileURL = "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-29T23.43.36Z/pricediscgroup/1.csv";
            //var fileURL = "https://testjaydenfiledev.blob.core.windows.net/dataverse-harrisfarmua-unq38184a8797ecee119046002248932/2024-10-30T12.43.37Z/inventtable/2023.csv";

            //var sqlMetadata = await _cdmService.GetCDMEntityMetadata(url, "pricediscgroup");
            //var sqlMetadata = await _cdmService.GetCDMEntityMetadata(url, "inventtable");
            var sqlMetadata = await _cdmService.GetCDMEntityMetadata(url);

            var tableList = new List<SqlCdmTable>();

            foreach (var table in sqlMetadata)
            {
                //if (!table.TableName.Contains("pricediscgroup") && !table.TableName.Contains("inventtable"))
                if (!table.TableName.Contains("pricediscgroup"))
                {
                    continue;
                }

                var stgTable = table.CopyAsStagingTable();

                _sqlSchemaManagementService.CreateOrUpdateTable(table);

                _sqlSchemaManagementService.CreateOrUpdateTable(stgTable);

                tableList.Add(table);

            }

            foreach (var table in tableList)
            {
                var tableUrls = fileURLs[table.TableName];

                var stgTable = table.CopyAsStagingTable();

                var sqlConnection = _sqlPersistence.GetSQLConnection();
                _sqlPersistence.TruncateTable(stgTable.TableName, sqlConnection);

                foreach (var fileURL in tableUrls)
                {
                    await _dataSyncService.SyncStorageAccountFile(fileURL);
                }

                var mergeScript = _sqlSchemaManagementService.GenerateMergeCode(table, stgTable);

                _sqlPersistence.ExecuteNonQuery(mergeScript, sqlConnection);
            }

            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
