using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Storage;
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
    public class CDMService : ICDMService
    {
        private readonly ILogger<CDMService> _logger;

        public CDMService(ILogger<CDMService> logger)
        {
            _logger = logger;
        }

        public SqlCdmTable GetCDMEntityFromStorageAccountURL(AzureStorageURL storageURL)
        {
            return new SqlCdmTable(storageURL.GetBlobParentFolderName());
        }

        public List<string> GetCDMEntityList(string manifestURL = "")
        {
            throw new NotImplementedException();
        }

        public async Task GetCDMEntityMetadataList(string manifestURL = "")
        {
            // TODO First attempt using hard coded values

            // TODO refer to ADLSContext in CDMUtil

            //MSITokenProvider MSITokenProvider = new MSITokenProvider($"https://{adlsContext.StorageAccount}/", adlsContext.TenantId);

            //var cdmCorpus = new CdmCorpusDefinition();

            //cdmCorpus.Storage.Mount("adls", new ADLSAdapter(
            //  adlsContext.StorageAccount, // Hostname.
            //  rootFolder + localFolder, // Root.
            //  MSITokenProvider
            //));

            return;
        }
    }
}
