using Microsoft.Extensions.Logging;

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

        public DataSyncService(ILogger<DataSyncService> logger, IAzureStorageAccountPersistence azureStorageAccountPersistence)
        {
            _logger = logger;
            _azureStorageAccountPersistence = azureStorageAccountPersistence;
        }

        public async Task SyncStorageAccountFile(string fileURL)
        {
            var storageURL = new AzureStorageURL(fileURL);

            var streamReader = await _azureStorageAccountPersistence.GetStreamReaderFromURL(storageURL);
        }
    }
}
