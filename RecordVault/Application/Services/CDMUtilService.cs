using CDMUtil.Context.ObjectDefinitions;
using CDMUtil.Manifest;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Application.Services
{
    public class CDMUtilService : ICDMService
    {
        private readonly ILogger<CDMUtilService> _logger;

        public CDMUtilService(ILogger<CDMUtilService> logger)
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

        public async Task<List<SQLMetadata>> GetCDMEntityMetadataList(string manifestURL = "")
        {
            var appConfigurations = GetAppConfigurations(manifestURL);

            List<SQLMetadata> metadataList = new List<SQLMetadata>();
            await ManifestReader.manifestToSQLMetadata(appConfigurations, metadataList, _logger, appConfigurations.rootFolder);

            return metadataList;
        }

        private AppConfigurations GetAppConfigurations(string manifestURLParam = "")
        {
            var manifestURL = manifestURLParam;

            if (manifestURL.IsNullOrEmpty())
            {
                manifestURL = Environment.GetEnvironmentVariable("CDMManifestURL") ?? "";
            }

            if (manifestURL.IsNullOrEmpty())
            {
                throw new ArgumentException("Manifest URL not provided");
            }

            var tenentId = Environment.GetEnvironmentVariable("AzureTenantId") ?? "";
            var accessKey = ""; // This is blank to make CDMUtil use MSAuth

            var appConfigurations = new AppConfigurations (tenentId, manifestURL, accessKey);

            return appConfigurations;
        }
    }
}
