using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using Microsoft.CommonDataModel.ObjectModel.Utilities.Network;
using Microsoft.Extensions.Logging;

using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;
using RecordVault.Infrastructure.Persistence;

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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task GetCDMEntityMetadataList(string manifestURL = "")
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // TODO First attempt using hard coded values
            // May need to use a TokenProviderFactory to get the token provider

            // TODO refer to ADLSContext in CDMUtil

            var storageURL = new AzureStorageURL(manifestURL);

            var containerURL = storageURL.StorageAccountUri().ToString() + storageURL.Container;
            if (!containerURL.EndsWith("/"))
            {
                containerURL = containerURL.Remove(containerURL.Length - 1);
            }

            var tenantId = Environment.GetEnvironmentVariable("AzureTenantId") ?? "";
            TokenProviderAsync msitokenProvider = new AzureTokenProvider(tenantId, storageURL.StorageAccountUri().ToString());

            var cdmCorpus = new CdmCorpusDefinition();

            //cdmCorpus.Storage.Mount("adls", new ADLSAdapter(
            //  storageURL.StorageAccountUri().ToString(), // Hostname.
            //  storageURL.BlobFolder, // Root.
            //  msitokenProvider // Token provider.
            //));

            cdmCorpus.Storage.Mount("adls", new ADLSAdapter(
              containerURL, // Hostname.
              msitokenProvider // Token provider.
            ));

            cdmCorpus.Storage.DefaultNamespace = "adls";

            var manifestPath = storageURL.GetBlobPath();

            CdmManifestDefinition manifest = await cdmCorpus.FetchObjectAsync<CdmManifestDefinition>(storageURL.BlobFolder + storageURL.BlobName);

            return;
        }
    }
}
